namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Erreur, impossible de substituer une méthode créée dynamiquement pendant l'exécution<br/>
    /// Pour l'instant, cette implémentation est en cours
    /// </summary>
    public class CantHookDynamicMethodException : HookManagerException
    {
        private readonly string _nomMethod;

        /// <summary>
        /// Erreur, impossible de substituer une méthode créée dynamiquement pendant l'exécution<br/>
        /// Pour l'instant, cette implémentation est en cours
        /// </summary>
        internal CantHookDynamicMethodException(string nomMethode) : base()
        {
            _nomMethod = nomMethode;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "Impossible de substituer une méthode dynamique pour l'instant";
            }
        }

        /// <summary>
        /// Nom de la méthode en cause
        /// </summary>
        public string NomMethode
        {
            get
            {
                return _nomMethod;
            }
        }
    }
}
