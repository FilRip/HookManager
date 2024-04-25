using System;
using System.Runtime.Serialization;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, impossible de remplacer une méthode d'un assembly compilé avec l'option "JIT Optimized"
    /// </summary>
    [Serializable()]
    public class CantHookJitOptimizedException : HookManagerException
    {
        private readonly string _nomAssembly;
        private readonly string _nomMethode;

        /// <summary>
        /// Erreur, impossible de remplacer une méthode d'un assembly compilé avec l'option "JIT Optimized"
        /// </summary>
        /// <param name="nomAssembly">Nom de l'Assembly en cause</param>
        internal CantHookJitOptimizedException(string nomAssembly) : base()
        {
            _nomAssembly = nomAssembly;
        }

        /// <summary>
        /// Erreur, impossible de remplacer une méthode d'un assembly compilé avec l'option "JIT Optimized"
        /// </summary>
        /// <param name="nomAssembly">Nom de l'Assembly en cause</param>
        /// <param name="nomMethode">Nom de la méthode en cause</param>
        internal CantHookJitOptimizedException(string nomAssembly, string nomMethode) : this(nomAssembly)
        {
            _nomMethode = nomMethode;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Vous ne pouvez pas substituer la méthode '{_nomMethode}' de l'assembly '{_nomAssembly}', l'optimiseur JIT est activé avec un débuggeur attaché." + Environment.NewLine +
                    "Décochez le JIT Optimiseur dans les propriétés du projet de cet assembly avant recompilation, ou détacher le débuggeur." + Environment.NewLine +
                    "Cette configuration ne supporte pas non plus les méthodes retournant une valeur (différent de void ou Sub en VB). Décochez l'optimiseur JIT ou utilisez AjouterGACHook" + Environment.NewLine +
                    "Il n'est pas possible non plus de substituer un constructeur avec le JIT activé";
            }
        }

        /// <summary>
        /// Nom de l'Assembly en cause
        /// </summary>
        public string NomAssembly
        {
            get
            {
                return _nomAssembly;
            }
        }

        /// <summary>
        /// Nom de la méthode en cause
        /// </summary>
        public string NomMethode
        {
            get
            {
                return _nomMethode;
            }
        }

        /// <inheritdoc/>
        protected CantHookJitOptimizedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
