using System;
using System.Runtime.Serialization;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Classe de base pour toutes erreurs de la librairie présente ; HookManager
    /// </summary>
    [Serializable()]
    public abstract class HookManagerException : Exception
    {
        /// <inheritdoc/>
        protected HookManagerException() : base() { }

        /// <inheritdoc/>
        protected HookManagerException(string message) : base(message) { }

        /// <inheritdoc/>
        protected HookManagerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
