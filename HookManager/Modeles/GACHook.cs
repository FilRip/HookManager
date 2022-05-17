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
    /// Classe gérant le remplacement d'une méthode managée d'une librairie du GAC (Global Assembly Cache)<br/>
    /// Exemple : toutes méthodes du namespace "System" (du net framework)
    /// </summary>
    /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
    public sealed class GACHook
    {
        #region Private properties

        private byte[] FromPtrData;
        private readonly object _threadSafe = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// La méthode originale du GAC
        /// </summary>
        public MethodInfo FromMethod { get; }

        /// <summary>
        /// La méthode servant de substitution
        /// </summary>
        public MethodInfo ToMethod { get; }

        /// <summary>
        /// Indique si la substitution est active ou non
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
        /// Initialise une substitution d'une méthode d'un assembly du GAC
        /// </summary>
        /// <param name="from">La méthode du GAC à substituer</param>
        /// <param name="to">La méthode a appeler lorsque l'on appel la méthode du GAC</param>
        internal GACHook(MethodInfo from, MethodInfo to)
        {
            if (!from.IsStatic)
            {
                if (to.GetParameters().Length == 0)
                    throw new MissingDefaultArgument(to.Name, true);

                if ((to.GetParameters()[0].ParameterType != typeof(object)) && (to.GetParameters()[0].ParameterType != from.DeclaringType))
                    throw new MissingDefaultArgument(to.Name, true);

                if (from.GetParameters().Length > to.GetParameters().Length - 1)
                    throw new NotEnoughArgument(from.GetParameters().Length, to.GetParameters().Length - 1);
            }
            if (!to.IsStatic)
                throw new MethodDestinationNotStatic();

            FromMethod = from;
            ToMethod = to;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applique la substitution. A appeler que la 1ère fois
        /// </summary>
        public void Apply()
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

        /// <summary>
        /// Active la substitution. La méthode ne sert que pour toutes les autres fois, sauf la 1ère fois où il faut appeler la méthode "Apply()"
        /// </summary>
        public void ReApply()
        {
            lock (_threadSafe)
            {
                if (!IsEnabled)
                {
                    if (_existingPtrData == null)
                        throw new NullReferenceException(
                            "ExistingPtrData was null. Call ManagedHook.Remove() to populate the data.");

                    WinAPI.VirtualProtect(_fromPtr, (IntPtr)5, 0x40, out uint x);

                    for (var i = 0; i < _existingPtrData.Length; i++)
                        // Instead write back the contents from the existing ptr data first applied.
                        Marshal.WriteByte(_fromPtr, i, _existingPtrData[i]);

                    WinAPI.VirtualProtect(_fromPtr, (IntPtr)5, x, out _);
                    IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Désactive la substitution de la méthode GAC vers la méthode de substitution
        /// </summary>
        public void Remove()
        {
            lock (_threadSafe)
            {
                if (IsEnabled)
                {
                    if (_originalPtrData == null)
                        throw new NullReferenceException(
                            "OriginalPtrData was null. Call ManagedHook.Apply() to populate the data.");

                    // Unlock memory for readwrite
                    WinAPI.VirtualProtect(_fromPtr, (IntPtr)5, 0x40, out uint x);

                    _existingPtrData = new byte[_originalPtrData.Length];

                    for (var i = 0; i < _originalPtrData.Length; i++)
                    {
                        // Add to the existing ptr data variable so we can reapply if need be.
                        _existingPtrData[i] = Marshal.ReadByte(_fromPtr, i);

                        Marshal.WriteByte(_fromPtr, i, _originalPtrData[i]);
                    }

                    WinAPI.VirtualProtect(_fromPtr, (IntPtr)5, x, out _);
                    IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Appel la méthode originale du GAC
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Call<T>(object instance, params object[] args) where T : class
        {
            lock (_threadSafe)
            {
                Remove();
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
                    if ((Debugger.IsAttached) && (HookPool.GetInstance().ModeDebugInterne)) Debugger.Break();
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

            FromPtrData = new byte[32];
            Marshal.Copy(_fromPtr, FromPtrData, 0, 32);

            WinAPI.VirtualProtect(_fromPtr, (IntPtr)5, 0x40, out uint x);

            if (IntPtr.Size == 8)
            {
                // x64

                _originalPtrData = new byte[13];

                // 13
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

                // 6
                Marshal.Copy(_fromPtr, _originalPtrData, 0, 6);

                Marshal.WriteByte(_fromPtr, 0, 0xe9);
                Marshal.WriteInt32(_fromPtr, 1, _toPtr.ToInt32() - _fromPtr.ToInt32() - 5);
                Marshal.WriteByte(_fromPtr, 5, 0xc3);
            }

            WinAPI.VirtualProtect(_fromPtr, (IntPtr)5, x, out _);
        }

        #endregion
    }
}
