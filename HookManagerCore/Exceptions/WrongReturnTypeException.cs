namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Erreur, le type de retour de la méthode de remplacement n'est pas le même (ou héritant) que le type de retour de la méthode d'origine/remplacée
    /// </summary>
    public class WrongReturnTypeException : HookManagerException
    {
        private readonly Type _typeSource;
        private readonly Type _typeDestination;

        /// <summary>
        /// Erreur, le type de retour de la méthode de remplacement n'est pas le même (ou héritant) que le type de retour de la méthode d'origine/remplacée
        /// </summary>
        /// <param name="typeSource">Type d'origine</param>
        /// <param name="typeDestination">Type de remplacement</param>
        internal WrongReturnTypeException(Type typeSource, Type typeDestination) : base()
        {
            _typeSource = typeSource;
            _typeDestination = typeDestination;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Le type de retour de la méthode à substituer n'est pas le même, ou est incompatible, avec la méthode servant de substitution. Type de la méthode source : {_typeSource}, type de la méthode destination : {_typeDestination}";
            }
        }

        /// <summary>
        /// Type d'origine
        /// </summary>
        public Type TypeSource
        {
            get
            {
                return _typeSource;
            }
        }

        /// <summary>
        /// Type de remplacement
        /// </summary>
        public Type TypeRemplacement
        {
            get
            {
                return _typeDestination;
            }
        }
    }
}
