using System;
using System.Runtime.Serialization;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, le type de retour de la méthode de remplacement n'est pas le même (ou héritant) que le type de retour de la méthode d'origine/remplacée
    /// </summary>
    [Serializable()]
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
                return $"The return type of the method to replace is not the same, or can't be cast, with the return type of the replacement method. Return type of method to replace : {_typeSource}, return type of the new method : {_typeDestination}";
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

        /// <inheritdoc/>
        protected WrongReturnTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
