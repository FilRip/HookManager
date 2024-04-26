using System;
using System.Runtime.Serialization;

namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Erreur, la "nouvelle" méthode de remplacement doit être static<br/>
    /// </summary>
    [Serializable()]
    public class MethodDestinationNotStaticException : HookManagerException
    {
        internal MethodDestinationNotStaticException() : base()
        {
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "La méthode destination doit être déclarée static (shared en VBNET)";
            }
        }

        /// <inheritdoc/>
        protected MethodDestinationNotStaticException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
