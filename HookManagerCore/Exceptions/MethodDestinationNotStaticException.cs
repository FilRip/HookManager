namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Erreur, la "nouvelle" méthode de remplacement doit être static<br/>
    /// </summary>
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
    }
}
