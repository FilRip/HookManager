using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, le type fournit, pour un Remplacement de toutes ou parties des méthodes d'une interface, n'est pas un type d'Interface
    /// </summary>
    [Serializable()]
    public class NotInterface : HookManagerException
    {
        private readonly Type _type;

        /// <summary>
        /// Erreur, le type fournit, pour un Remplacement de toutes ou parties des méthodes d'une interface, n'est pas un type d'Interface
        /// </summary>
        /// <param name="TypeFournit">Type fournit</param>
        internal NotInterface(Type TypeFournit) : base()
        {
            _type = TypeFournit;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Le type fournit ({_type?.Name}) n'est pas une Interface";
            }
        }

        /// <summary>
        /// Type fournit
        /// </summary>
        public Type TypeFournit
        {
            get
            {
                return _type;
            }
        }
    }
}
