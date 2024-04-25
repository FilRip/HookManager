using System;
using System.Runtime.Serialization;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Type d'erreur générée lors de la tentative de substituer une propriété
    /// </summary>
    [Serializable()]
    public enum EErrorCodePropertyHook
    {
        /// <summary>
        /// Pas d'erreur
        /// </summary>
        None = 0,
        /// <summary>
        /// Remplacement inutile = aucune méthode Get ni aucune methode Set
        /// </summary>
        UselessReplacement = 1,
        /// <summary>
        /// La propriété n'a pas de méthode Get
        /// </summary>
        NoGet = 2,
        /// <summary>
        /// La propriété n'a pas de méthode Set
        /// </summary>
        NoSet = 3,
        /// <summary>
        /// Le nom, le type ou le namespace, de la propriété n'a pas pu être trouvée
        /// </summary>
        PropertyNotFound = 4,
    };

    /// <summary>
    /// Une erreur est subvenue lors de la tentative de substituer une propriété
    /// </summary>
    [Serializable()]
    public class HookPropertyException : HookManagerException
    {
        private readonly string _methodGet, _methodSet;
        private readonly string _propertySource;
        private readonly EErrorCodePropertyHook _errorCode;

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return _errorCode switch
                {
                    EErrorCodePropertyHook.UselessReplacement => $"Unable to replace property {_propertySource} : There is no get or set, useless replacement",
                    EErrorCodePropertyHook.NoGet => $"Unable to replace property getter {_propertySource}, this property has no getter",
                    EErrorCodePropertyHook.NoSet => $"Unable to replace property setter {_propertySource}, this property has no setter",
                    EErrorCodePropertyHook.PropertyNotFound => $"Unable to find property {_propertySource}. Check the full name (<Namespace>.<Class>.<PropertyName>)",
                    _ => "Unknown error during trying to replace getter and/or setter of a property",
                };
            }
        }

        /// <summary>
        /// Une erreur est subvenue lors de la tentative de substituer une propriété
        /// </summary>
        internal HookPropertyException(string propertySource, string methodGet, string methodSet, EErrorCodePropertyHook errorCode) : base()
        {
            _propertySource = propertySource;
            _methodGet = methodGet;
            _methodSet = methodSet;
            _errorCode = errorCode;
        }

        /// <summary>
        /// Type d'erreur rencontré lors d'une tentative de substituer une propriété
        /// </summary>
        public EErrorCodePropertyHook ErrorCode
        {
            get { return _errorCode; }
        }

        /// <summary>
        /// Méthode de remplacement pour le "get" de la propriété
        /// </summary>
        public string NameMethodGet
        {
            get { return _methodGet; }
        }

        /// <summary>
        /// Méthode de remplacement pour le "set" de la propriété
        /// </summary>
        public string NameMethodSet
        {
            get { return _methodSet; }
        }

        /// <summary>
        /// Propriété que vous tentez de remplacer
        /// </summary>
        public string NameProprerty
        {
            get { return _propertySource; }
        }

        /// <inheritdoc/>
        protected HookPropertyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
