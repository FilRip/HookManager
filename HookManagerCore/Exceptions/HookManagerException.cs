namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Classe de base pour toutes erreurs de la librairie présente ; HookManager
    /// </summary>
    public abstract class HookManagerException : Exception
    {
        /// <inheritdoc/>
        protected HookManagerException() : base() { }

        /// <inheritdoc/>
        protected HookManagerException(string message) : base(message) { }
    }
}
