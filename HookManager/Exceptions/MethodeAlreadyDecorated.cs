using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, la méthode que vous tentez de décorer est déjà décorée, consultez HookPool pour la liste
    /// </summary>
    [Serializable()]
    public class MethodeAlreadyDecorated : HookManagerException
    {
        private readonly string _nomMethode;

        /// <summary>
        /// Erreur, la méthode que vous tentez de décorer est déjà décorée, consultez HookPool pour la liste
        /// </summary>
        internal MethodeAlreadyDecorated(string nomMethode) : base()
        {
            _nomMethode = nomMethode;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Cette méthode ({_nomMethode}) est déjà décorée (ou constructeur). On ne peut décorer qu'une seule fois chaque méthode (ou constructeur).";
            }
        }

        /// <summary>
        /// Nom de la méthode en cause
        /// </summary>
        public string NomMethode
        {
            get { return _nomMethode; }
        }
    }
}
