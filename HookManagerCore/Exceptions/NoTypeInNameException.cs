namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Le nom de la méthode n'est pas correct. Spécifiez le type (et l'espace de nom au besoin)
    /// </summary>
    public class NoTypeInNameException : HookManagerException
    {
        private readonly string _nomMethode;

        /// <summary>
        /// Le nom de la méthode n'est pas correct. Spécifiez le type (et l'espace de nom au besoin)
        /// </summary>
        internal NoTypeInNameException(string nomMethode) : base()
        {
            _nomMethode = nomMethode;
        }

        /// <summary>
        /// Nom de la méthode en cause
        /// </summary>
        public string NomMethode
        {
            get
            {
                return _nomMethode;
            }
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Le nom de l'élément {_nomMethode} n'a pas de type, ou le type n'a pas été trouvé. Vous devez spécifier comme nom : " + Environment.NewLine + "Type.NomElement" + Environment.NewLine + "(séparé par un point) et l'espace de nom, au besoin, exemple : " + Environment.NewLine + "Namespace.Type.NomElement";
            }
        }
    }
}
