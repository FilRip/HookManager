using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Exception lors de la tentative d'ajouter une décoration à une méthode
    /// </summary>
    [Serializable()]
    public class DecorationMethodesException : HookManagerException
    {
        private readonly string _methodeAvant;
        private readonly string _methodeApres;

        /// <summary>
        /// Exception lors de la tentative d'ajouter une décoration à une méthode
        /// </summary>
        /// <param name="methodeAvant">Nom de la méthode à exécuter avant, si présente</param>
        /// <param name="methodeApres">Nom de la méthode à exécuter après, si présente</param>
        internal DecorationMethodesException(string methodeAvant, string methodeApres) : base()
        {
            _methodeAvant = methodeAvant;
            _methodeApres = methodeApres;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "Au moins une des méthodes spécifiées pour la décoration est introuvable. Ou les 2 sont manquantes (décoration inutile si rien avant et rien après)";
            }
        }

        /// <summary>
        /// Nom de la méthode à exécuter avant, si présente
        /// </summary>
        public string NomMethodeAvant
        {
            get { return _methodeAvant; }
        }

        /// <summary>
        /// Nom de la méthode à exécuter après, si présente
        /// </summary>
        public string NomMethodeApres
        {
            get { return _methodeApres; }
        }
    }
}
