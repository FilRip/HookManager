using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, la "nouvelle" méthode de remplacement doit être static<br/>
    /// </summary>
    [Serializable()]
    public class MethodDestinationNotStatic : HookManagerException
    {
        internal MethodDestinationNotStatic() : base()
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
    }
}
