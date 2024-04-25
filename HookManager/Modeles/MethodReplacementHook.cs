using System.Reflection;
using System.Runtime.CompilerServices;

using HookManager.Helpers;

namespace HookManager.Modeles
{
    /// <summary>
    /// Replace definetly a method by another one. The older one is no more accessible
    /// </summary>
    public sealed class MethodReplacementHook
    {
        private readonly MethodInfo _methodFrom, _methodTo;
        private readonly uint _numHook;
        private bool _actif;

        /// <summary>
        /// Internal Id of this replacement in HookPool
        /// </summary>
        public uint NumHook
        {
            get
            {
                return _numHook;
            }
        }

        /// <summary>
        /// Method to replace
        /// </summary>
        public MethodInfo MethodFrom
        {
            get
            {
                return _methodFrom;
            }
        }

        /// <summary>
        /// New method
        /// </summary>
        public MethodInfo MethodTo
        {
            get
            {
                return _methodTo;
            }
        }

        internal MethodReplacementHook(uint numHook, MethodInfo methodeSource, MethodInfo methodeDestination, bool autoActiver = true)
        {
            _numHook = numHook;
            _methodFrom = methodeSource;
            _methodTo = methodeDestination;

            Prepare(autoActiver);
        }

        private void Prepare(bool autoActiver)
        {
            RuntimeHelpers.PrepareMethod(_methodFrom.MethodHandle);
            RuntimeHelpers.PrepareMethod(_methodTo.MethodHandle);

            if (autoActiver)
                RemplaceMethode();
        }

        /// <summary>
        /// Active the replacement of method
        /// </summary>
        /// <remarks>Can't be revert</remarks>
        public void RemplaceMethode()
        {
            if (!_actif)
            {
                _methodFrom.ReplaceManagedMethod(_methodTo);
                _actif = true;
            }
        }

        /// <summary>
        /// Does the replacement is active or not
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return _actif;
            }
        }
    }
}
