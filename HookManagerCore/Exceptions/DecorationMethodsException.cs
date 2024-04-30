namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Exception lors de la tentative d'ajouter une décoration à une méthode
    /// </summary>
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
    }
}
