using System;
using System.Runtime.Serialization;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Exception lors de la tentative d'ajouter une décoration à une méthode
    /// </summary>
    [Serializable()]
    public class DecorationMethodsException : HookManagerException
    {
        /// <summary>
        /// Exception lors de la tentative d'ajouter une décoration à une méthode
        /// </summary>
        internal DecorationMethodsException() : base() { }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "Aucune méthode de décoration spécifiée.";
            }
        }

        /// <inheritdoc/>
        protected DecorationMethodsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
