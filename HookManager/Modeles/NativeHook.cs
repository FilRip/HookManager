using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HookManager.Helpers;

namespace HookManager.Modeles
{
    /// <summary>
    /// Classe gérant le remplacement d'une méthode native (API Windows)
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
        /// Initialise une substitution d'une méthode native vers une méthode managée
        /// </summary>
        /// <param name="from">Objet NativeMethod contenant le nom du module et le nom de la méthode native à substituer</param>
        /// <param name="to">Méthode managée qui remplacera la méthode Native</param>
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
        /// Active la substitution de la méthode Native (à faire qu'une fois, la toute première fois)
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
        /// Désactive la substitution de la méthode Native
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
        /// Appel la méthode Native originale (avant substitution)
        /// </summary>
        /// <typeparam name="T">Delegué représentant la signature de la méthode native</typeparam>
        /// <typeparam name="V">Le type de retour</typeparam>
        /// <param name="args">Paramètres pour la méthode native d'origine, si elle en a</param>
        /// <remarks>Appeler la méthode "parente" native ne supporte PAS le multithread</remarks>
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
    /// Classe représentant la méthode native d'origine
    /// </summary>
    /// <remarks>
    /// Objet représentant la méthode Native à substituer
    /// </remarks>
    /// <param name="method">Nom de la méthode</param>
    /// <param name="module">Nom du module (avec son extension)</param>
    public class NativeMethod(string method, string module)
    {
        #region Fields

        /// <summary>
        /// Le pointeur (adresse mémoire) de la méthode Native
        /// </summary>
        public IntPtr Address { get; set; }

        /// <summary>
        /// Le nom de la méthode
        /// </summary>
        public string Method { get; set; } = method;

        /// <summary>
        /// Le nom du module (avec son extension)
        /// </summary>
        public string ModuleName { get; set; } = module;

        #endregion

        #region Constructors

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
