using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HookManager.Helpers;

namespace HookManager.Modeles
{
    /// <summary>
    /// Class manage one hook on native method (api Windows)
    /// </summary>
    public sealed class NativeHook
    {
        #region Private Properties

        private bool _isEnabled;
        private readonly object _threadSafe = new();
        private bool _firstRedirect = true;

        #endregion

        #region Public Properties

        /// <summary>
        /// La méthode native à substituer
        /// </summary>
        public NativeMethod FromMethod { get; }

        /// <summary>
        /// La méthode managée qui se substitue à la méthode Native
        /// </summary>
        public MethodInfo ToMethod { get; }

        /// <summary>
        /// La substitution est-elle activée ?
        /// </summary>
        public bool IsEnabled { get { return _isEnabled; } }

        #endregion

        #region Fields

        /// <summary>
        ///     The module cache.
        /// </summary>
        private readonly Dictionary<string, IntPtr> _modules = [];

        /// <summary>
        ///     The function pointer data for the redirect, created after <see cref="Disable" /> is called.
        /// </summary>
        private byte[] _existingPtrData;

        /// <summary>
        ///     The original function pointer data.
        /// </summary>
        private byte[] _originalPtrData;

        #endregion

        #region Constructors

        /// <summary>
        /// Class manage one hook on native method (api Windows)
        /// </summary>
        /// <param name="from">Instance of <see cref="NativeMethod"/> contains name of library and name of method to replace</param>
        /// <param name="to">Managed method of replacement</param>
        internal NativeHook(NativeMethod from, MethodInfo to)
        {
            FromMethod = from;
            FromMethod.Address = GetAddress(from.ModuleName, from.Method);

            if (FromMethod.Address == IntPtr.Zero)
                throw new ArgumentNullException(nameof(from));
            ToMethod = to ?? throw new ArgumentNullException(nameof(to));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Enable the replacement
        /// </summary>
        public void Enable()
        {
            lock (_threadSafe)
            {
                if (!_isEnabled)
                {
                    if (_firstRedirect)
                    {
                        Redirect(FromMethod, ToMethod.MethodHandle);
                        _isEnabled = true;
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
                if (!_isEnabled)
                {
                    var fromPtr = FromMethod.Address;

                    WinApi.VirtualProtect(fromPtr, (IntPtr)5, 0x40, out uint x);

                    for (var i = 0; i < _existingPtrData.Length; i++)
                        // Instead write back the contents from the existing ptr data first applied.
                        Marshal.WriteByte(fromPtr, i, _existingPtrData[i]);

                    WinApi.VirtualProtect(fromPtr, (IntPtr)5, x, out _);
                    _isEnabled = true;
                }
            }
        }

        /// <summary>
        /// Disable the replacement
        /// </summary>
        public void Disable()
        {
            lock (_threadSafe)
            {
                if (_isEnabled)
                {
                    var fromPtr = FromMethod.Address;

                    // Unlock memory for readwrite
                    WinApi.VirtualProtect(fromPtr, (IntPtr)5, 0x40, out uint x);

                    _existingPtrData = new byte[_originalPtrData.Length];

                    for (var i = 0; i < _originalPtrData.Length; i++)
                    {
                        // Add to the existing ptr data variable so we can reapply if need be.
                        _existingPtrData[i] = Marshal.ReadByte(fromPtr, i);

                        Marshal.WriteByte(fromPtr, i, _originalPtrData[i]);
                    }

                    WinApi.VirtualProtect(fromPtr, (IntPtr)5, x, out _);
                    _isEnabled = false;
                }
            }
        }

        /// <summary>
        /// Call the original method (not the managed one but the replaced)
        /// </summary>
        /// <typeparam name="T">Delegate for the original method</typeparam>
        /// <typeparam name="V">Type of return of the original method</typeparam>
        /// <param name="args">Parameters for the original method</param>
        /// <remarks>Call the original native method does not support multithread</remarks>
        public V CallOriginalMethod<T, V>(params object[] args)
            where T : class
            where V : class
        {
            lock (_threadSafe)
            {
                Disable();
                var ret = Marshal.GetDelegateForFunctionPointer(FromMethod.Address, typeof(T)).DynamicInvoke(args) as V;
                ReApply();
                return ret;
            }
        }

        #endregion

        #region Private Methods

        private IntPtr GetAddress(string module, string method)
        {
            if (!_modules.ContainsKey(module)) _modules.Add(module, WinApi.LoadLibraryEx(module, IntPtr.Zero, 0));
            return WinApi.GetProcAddress(_modules[module], method);
        }

        private void Redirect(NativeMethod from, RuntimeMethodHandle to)
        {
            RuntimeHelpers.PrepareMethod(to);

            var fromPtr = from.Address;
            var toPtr = to.GetFunctionPointer();

            byte[] FromPtrData = new byte[32];
            Marshal.Copy(fromPtr, FromPtrData, 0, 32);

            WinApi.VirtualProtect(fromPtr, (IntPtr)5, 0x40, out uint x);

            if (IntPtr.Size == 8)
            {
                // x64
                _originalPtrData = new byte[13];

                Marshal.Copy(fromPtr, _originalPtrData, 0, 13);

                Marshal.WriteByte(fromPtr, 0, 0x49);
                Marshal.WriteByte(fromPtr, 1, 0xbb);

                Marshal.WriteInt64(fromPtr, 2, toPtr.ToInt64());

                Marshal.WriteByte(fromPtr, 10, 0x41);
                Marshal.WriteByte(fromPtr, 11, 0xff);
                Marshal.WriteByte(fromPtr, 12, 0xe3);

            }
            else if (IntPtr.Size == 4)
            {
                // x86

                _originalPtrData = new byte[6];

                Marshal.Copy(fromPtr, _originalPtrData, 0, 6);

                Marshal.WriteByte(fromPtr, 0, 0xe9);
                Marshal.WriteInt32(fromPtr, 1, toPtr.ToInt32() - fromPtr.ToInt32() - 5);
                Marshal.WriteByte(fromPtr, 5, 0xc3);
            }

            WinApi.VirtualProtect(fromPtr, (IntPtr)5, x, out _);
        }

        #endregion
    }

    /// <summary>
    /// Class with all informations about the native method to replace
    /// </summary>
    /// <param name="method">Name of the native method</param>
    /// <param name="module">Name of the library that contains the native method (with extension)</param>
    public class NativeMethod(string method, string module)
    {
        #region Fields

        /// <summary>
        /// The pointer to the original native method
        /// </summary>
        public IntPtr Address { get; set; }

        /// <summary>
        /// Name of the native method
        /// </summary>
        public string Method { get; set; } = method;

        /// <summary>
        /// Name of the library that contains the native method (with extension)
        /// </summary>
        public string ModuleName { get; set; } = module;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ModuleName} - {Method} ({Address})";
        }

        #endregion
    }
}
