using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HookManager.Exceptions;
using HookManager.Helpers;

namespace HookManager.Modeles
{
    /// <summary>
    /// Class manage one replaced method in Assembly in GAC<br/>
    /// </summary>
    /// <remarks>Call the original method does not support multithread</remarks>
    public sealed class GacHook
    {
        #region Private properties

        private readonly object _threadSafe = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// The original method
        /// </summary>
        public MethodInfo FromMethod { get; }

        /// <summary>
        /// The method replacement
        /// </summary>
        public MethodInfo ToMethod { get; }

        /// <summary>
        /// Does the hook is enabled or not
        /// </summary>
        public bool IsEnabled { get; set; }

        private bool _firstRedirect = true;

        #endregion

        #region Fields

        private byte[] _existingPtrData;
        private byte[] _originalPtrData;

        private IntPtr _toPtr;
        private IntPtr _fromPtr;

        #endregion

        #region Constructors

        /// <summary>
        /// Class manage one replaced method in Assembly in GAC<br/>
        /// </summary>
        /// <param name="from">The original method, in GAC</param>
        /// <param name="to">The new method to call instead of the one in GAC</param>
        internal GacHook(MethodInfo from, MethodInfo to)
        {
            if (!from.IsStatic)
            {
                if (to.GetParameters().Length == 0)
                    throw new MissingDefaultArgumentException(to.Name, true);

                if ((to.GetParameters()[0].ParameterType != typeof(object)) && (to.GetParameters()[0].ParameterType != from.DeclaringType))
                    throw new MissingDefaultArgumentException(to.Name, true);

                if (from.GetParameters().Length > to.GetParameters().Length - 1)
                    throw new NotEnoughArgumentException(from.GetParameters().Length, to.GetParameters().Length - 1);
            }
            if (!to.IsStatic)
                throw new MethodDestinationNotStaticException();

            FromMethod = from;
            ToMethod = to;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable this hook
        /// </summary>
        public void Enable()
        {
            lock (_threadSafe)
            {
                if (!IsEnabled)
                {
                    if (_firstRedirect)
                    {
                        Redirect(FromMethod.MethodHandle, ToMethod.MethodHandle);
                        IsEnabled = true;
                        _firstRedirect = false;
                    }
                    else
                        ReApply();
                }
            }
        }

        private void ReApply()
        {
            lock (_threadSafe)
            {
                if (!IsEnabled)
                {
                    if (_existingPtrData == null)
                        throw new CommonHookManagerException("ExistingPtrData was null. Call ManagedHook.Remove() to populate the data.");

                    WinApi.VirtualProtect(_fromPtr, (IntPtr)5, 0x40, out uint x);

                    for (var i = 0; i < _existingPtrData.Length; i++)
                        // Instead write back the contents from the existing ptr data first applied.
                        Marshal.WriteByte(_fromPtr, i, _existingPtrData[i]);

                    WinApi.VirtualProtect(_fromPtr, (IntPtr)5, x, out _);
                    IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Disable this hook
        /// </summary>
        public void Disable()
        {
            lock (_threadSafe)
            {
                if (IsEnabled)
                {
                    if (_originalPtrData == null)
                        throw new CommonHookManagerException("OriginalPtrData was null. Call ManagedHook.Apply() to populate the data.");

                    // Unlock memory for readwrite
                    WinApi.VirtualProtect(_fromPtr, (IntPtr)5, 0x40, out uint x);

                    _existingPtrData = new byte[_originalPtrData.Length];

                    for (var i = 0; i < _originalPtrData.Length; i++)
                    {
                        // Add to the existing ptr data variable so we can reapply if need be.
                        _existingPtrData[i] = Marshal.ReadByte(_fromPtr, i);

                        Marshal.WriteByte(_fromPtr, i, _originalPtrData[i]);
                    }

                    WinApi.VirtualProtect(_fromPtr, (IntPtr)5, x, out _);
                    IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Call the original (replaced) method in GAC
        /// </summary>
        /// <typeparam name="T">Return type by the method, if not a void</typeparam>
        /// <param name="instance">Instance of current object or null if static method</param>
        /// <param name="args">Parameters of original method, if there is/are</param>
        /// <remarks>Call the original method does not support multithread</remarks>
        public T CallOriginalMethod<T>(object instance, params object[] args) where T : class
        {
            lock (_threadSafe)
            {
                Disable();
                try
                {
                    if (args.Length > FromMethod.GetParameters().Length)
                        Array.Resize(ref args, FromMethod.GetParameters().Length);
                    var ret = FromMethod.Invoke(instance, args) as T;
                    ReApply();
                    return ret;
                }
                catch (Exception)
                {
                    if ((Debugger.IsAttached) && (HookPool.GetInstance().ModeInternalDebug)) Debugger.Break();
                }

                ReApply();
            }
            return default;
        }

        #endregion

        #region Private Methods

        private void Redirect(RuntimeMethodHandle from, RuntimeMethodHandle to)
        {
            RuntimeHelpers.PrepareMethod(to);
            RuntimeHelpers.PrepareMethod(from);

            // Just in case someone calls apply twice or something, let's not get the same ptr for no reason.
            if (_fromPtr == default) _fromPtr = from.GetFunctionPointer();
            if (_toPtr == default) _toPtr = to.GetFunctionPointer();

            byte[] FromPtrData = new byte[32];
            Marshal.Copy(_fromPtr, FromPtrData, 0, 32);

            WinApi.VirtualProtect(_fromPtr, (IntPtr)5, 0x40, out uint x);

            if (IntPtr.Size == 8)
            {
                // x64

                _originalPtrData = new byte[13];

                Marshal.Copy(_fromPtr, _originalPtrData, 0, 13);

                Marshal.WriteByte(_fromPtr, 0, 0x49);
                Marshal.WriteByte(_fromPtr, 1, 0xbb);

                Marshal.WriteInt64(_fromPtr, 2, _toPtr.ToInt64());

                Marshal.WriteByte(_fromPtr, 10, 0x41);
                Marshal.WriteByte(_fromPtr, 11, 0xff);
                Marshal.WriteByte(_fromPtr, 12, 0xe3);

            }
            else if (IntPtr.Size == 4)
            {
                // x86

                _originalPtrData = new byte[6];

                Marshal.Copy(_fromPtr, _originalPtrData, 0, 6);

                Marshal.WriteByte(_fromPtr, 0, 0xe9);
                Marshal.WriteInt32(_fromPtr, 1, _toPtr.ToInt32() - _fromPtr.ToInt32() - 5);
                Marshal.WriteByte(_fromPtr, 5, 0xc3);
            }

            WinApi.VirtualProtect(_fromPtr, (IntPtr)5, x, out _);
        }

        #endregion
    }
}
