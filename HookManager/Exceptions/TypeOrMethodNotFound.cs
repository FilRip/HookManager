using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, le type, ou la méthode que vous demandez de remplacer, n'existe pas/n'a pas été trouvé<br/>
    /// Assurez-vous de l'ortographe, et que l'assembly a bien été chargé avant d'appeler le remplacement de la méthode
    /// </summary>
    [Serializable()]
    public class TypeOrMethodNotFound : HookManagerException
    {
        private readonly string _type, _methode;

        /// <summary>
        /// Erreur, le type, ou la méthode que vous demandez de remplacer, n'existe pas/n'a pas été trouvé<br/>
        /// Assurez-vous de l'ortographe, et que l'assembly a bien été chargé avant d'appeler le remplacement de la méthode
        /// </summary>
        /// <param name="leType">Nom du type dans lequel la méthode à été recherchée</param>
        /// <param name="laMethode">Nom de la méthode recherchée (non trouvée)</param>
        internal TypeOrMethodNotFound(string leType, string laMethode) : base()
        {
            _type = leType;
            _methode = laMethode;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override string Message
        {
            get
            {
                return $"Le type de l'objet ({_type}), ou la méthode spécifiée ({_methode}), n'ont pas été trouvés (ou la méthode n'est pas public ou private et static ou non static)";
            }
        }

        /// <summary>
        /// Nom du type dans lequel la méthode à été recherchée
        /// </summary>
        public string Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Nom de la méthode recherchée (non trouvée)
        /// </summary>
        public string Methode
        {
            get
            {
                return _methode;
            }
        }
    }
}
