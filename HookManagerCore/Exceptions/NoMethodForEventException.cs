namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Erreur rien à substituer pour un évènement
    /// </summary>
    public class NoMethodForEventException : HookManagerException
    {
        /// <inheritdoc/>
        public NoMethodForEventException() : base() { }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "Vous n'avez spécifié aucune méthode d'ajout ni aucune méthode de suppression d'un délégué à un Event. Les 2 sont facultatifs, mais il faut au moins 1 des deux";
            }
        }
    }
}
