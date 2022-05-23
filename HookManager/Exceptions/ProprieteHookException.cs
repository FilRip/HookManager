using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Type d'erreur générée lors de la tentative de substituer une propriété
    /// </summary>
    [Serializable()]
    public enum CODE_ERREUR_PROPRIETE_HOOK
    {
        /// <summary>
        /// Pas d'erreur
        /// </summary>
        Aucun = 0,
        /// <summary>
        /// Remplacement inutile = aucune méthode Get ni aucune methode Set
        /// </summary>
        Remplacement_inutile = 1,
        /// <summary>
        /// La propriété n'a pas de méthode Get
        /// </summary>
        Pas_de_get = 2,
        /// <summary>
        /// La propriété n'a pas de méthode Set
        /// </summary>
        Pas_de_set = 3,
        /// <summary>
        /// Le nom, le type ou le namespace, de la propriété n'a pas pu être trouvée
        /// </summary>
        Propriete_introuvable = 4,
    };

    /// <summary>
    /// Une erreur est subvenue lors de la tentative de substituer une propriété
    /// </summary>
    [Serializable()]
    public class ProprieteHookException : HookManagerException
    {
        private readonly string _methodeGet, _methodeSet;
        private readonly string _proprieteSource;
        private readonly CODE_ERREUR_PROPRIETE_HOOK _codeErreur;

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return _codeErreur switch
                {
                    CODE_ERREUR_PROPRIETE_HOOK.Remplacement_inutile => $"Impossible de remplacer la propriété {_proprieteSource} : il n'y a aucune méthode ni pour le getter, ni pour le setter ; le remplacement ne sert à rien",
                    CODE_ERREUR_PROPRIETE_HOOK.Pas_de_get => $"Impossible de remplacer le getter de la propriété {_proprieteSource}, cette propriété n'a pas de getter",
                    CODE_ERREUR_PROPRIETE_HOOK.Pas_de_set => $"Impossible de remplacer le setter de la propriété {_proprieteSource}, cette propriété n'a pas de setter",
                    CODE_ERREUR_PROPRIETE_HOOK.Propriete_introuvable => $"Impossible de trouver la propriété {_proprieteSource}. Vérifiez que le nom complet soit spécifié (<Namespace>.<Classe>.<NomPropriete>)",
                    _ => "Erreur pendant la tentative de remplacement d'une propriété",
                };
            }
        }

        /// <summary>
        /// Une erreur est subvenue lors de la tentative de substituer une propriété
        /// </summary>
        internal ProprieteHookException(string proprieteSource, string methodeGet, string methodeSet, CODE_ERREUR_PROPRIETE_HOOK codeErreur) : base()
        {
            _proprieteSource = proprieteSource;
            _methodeGet = methodeGet;
            _methodeSet = methodeSet;
            _codeErreur = codeErreur;
        }

        /// <summary>
        /// Type d'erreur rencontré lors d'une tentative de substituer une propriété
        /// </summary>
        public CODE_ERREUR_PROPRIETE_HOOK CodeErreur
        {
            get { return _codeErreur; }
        }

        /// <summary>
        /// Méthode de remplacement pour le "get" de la propriété
        /// </summary>
        public string MethodeGet
        {
            get { return _methodeGet; }
        }

        /// <summary>
        /// Méthode de remplacement pour le "set" de la propriété
        /// </summary>
        public string MethodeSet
        {
            get { return _methodeSet; }
        }

        /// <summary>
        /// Propriété que vous tentez de remplacer
        /// </summary>
        public string ProprieteSource
        {
            get { return _proprieteSource; }
        }
    }
}
