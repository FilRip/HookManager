using System;
using System.Reflection;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Le type de plateforme de compilation n'est pas supporté par cette librairie
    /// </summary>
    [Serializable()]
    public class PlateformeNonSupportee : HookManagerException
    {
        private readonly ProcessorArchitecture _plateforme;

        /// <summary>
        /// Le type de plateforme de compilation n'est pas supporté par cette librairie
        /// </summary>
        public PlateformeNonSupportee(ProcessorArchitecture plateforme) : base()
        {
            _plateforme = plateforme;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Ce type de plateforme ({_plateforme:G}) n'est pas supportée par cette librairie";
            }
        }

        /// <summary>
        /// Plateforme en cause, non supportée par cette librairie
        /// </summary>
        public ProcessorArchitecture Plateforme
        {
            get
            {
                return _plateforme;
            }
        }
    }
}
