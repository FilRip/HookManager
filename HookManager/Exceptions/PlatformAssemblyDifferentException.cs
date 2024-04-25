using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur plateformes de compilation différentes entres les assembly de la méthode à substituer et la méthode de substitution
    /// </summary>
    [Serializable()]
    public class PlatformAssemblyDifferentException : HookManagerException
    {
        private readonly ProcessorArchitecture _paFrom, _paTo;

        /// <summary>
        /// Erreur plateformes de compilation différentes entres les assembly de méthodes à substituer
        /// </summary>
        internal PlatformAssemblyDifferentException(ProcessorArchitecture paFrom, ProcessorArchitecture paTo) : base()
        {
            _paFrom = paFrom;
            _paTo = paTo;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "L'assembly contenant la méthode à remplacer et l'assembly contenant la méthode de substitution sont compilée pour des plateformes différentes. Les 2 assembly doivent avoir des plateformes compatible, exemples :" + Environment.NewLine +
                    "x86 avec AnyCPU ou x86" + Environment.NewLine + "x64 avec AnyCPU ou x64" + Environment.NewLine + "AnyCPU toutes les deux";
            }
        }

        /// <summary>
        /// Plateforme de compilation de l'assembly contenant la méthode à remplacer
        /// </summary>
        public ProcessorArchitecture PlateformeMethodeARemplacer
        {
            get
            {
                return _paFrom;
            }
        }

        /// <summary>
        /// Plateforme de compilation de l'assembly contenant la méthode servant de remplacement
        /// </summary>
        public ProcessorArchitecture PlateformeMethodeDeRemplacement
        {
            get
            {
                return _paTo;
            }
        }

        /// <inheritdoc/>
        protected PlatformAssemblyDifferentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
