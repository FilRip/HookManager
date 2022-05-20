using System.Reflection;
using System.Runtime.CompilerServices;

using HookManager.Helpers;

namespace HookManager.Modeles
{
    /// <summary>
    /// Remplace une méthode par une autre (l'ancienne devient inaccessible)
    /// </summary>
    public sealed class MethodeRemplacementHook
    {
        private readonly MethodInfo _methodFrom, _methodTo;
        private readonly uint _numHook;
        private bool _actif;

        /// <summary>
        /// Numéro identifiant la substitution dans le HookPool
        /// </summary>
        public uint NumHook
        {
            get
            {
                return _numHook;
            }
        }

        /// <summary>
        /// Méthode remplacée
        /// </summary>
        public MethodInfo MethodeRemplacee
        {
            get
            {
                return _methodFrom;
            }
        }

        /// <summary>
        /// Méthode de remplacement
        /// </summary>
        public MethodInfo MethodeDeRemplacement
        {
            get
            {
                return _methodTo;
            }
        }

        internal MethodeRemplacementHook(uint numHook, MethodInfo methodeSource, MethodInfo methodeDestination, bool autoActiver = true)
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
        /// Active le remplacement de la méthode
        /// </summary>
        public void RemplaceMethode()
        {
            if (!_actif)
            {
                _methodFrom.RemplaceMethodeManagee(_methodTo);
                _actif = true;
            }
        }

        /// <summary>
        /// Le remplacement de la méthode est=-il actif ou non
        /// </summary>
        public bool EstActif
        {
            get
            {
                return _actif;
            }
        }
    }
}
