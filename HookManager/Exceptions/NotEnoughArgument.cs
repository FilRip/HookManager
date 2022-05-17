using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, la méthode de remplacement ne dispose pas assez de paramètre, par rapport à sa méthode d'origine/remplacée
    /// </summary>
    [Serializable()]
    public class NotEnoughArgument : HookManagerException
    {
        private readonly int _nbParamSource, _nbParamDestination;

        /// <summary>
        /// Erreur, la méthode de remplacement ne dispose pas assez de paramètre, par rapport à sa méthode d'origine/remplacée
        /// </summary>
        /// <param name="nbParamSource">Nombre de paramètres de la méthode à remplacer</param>
        /// <param name="nbParamDestination">Nombre de paramètres de la méthode de remplacement</param>
        public NotEnoughArgument(int nbParamSource, int nbParamDestination) : base()
        {
            _nbParamSource = nbParamSource;
            _nbParamDestination = nbParamDestination;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "La méthode destination n'a pas assez d'arguments par rapport à la méthode source" + Environment.NewLine + $"Méthode source : {_nbParamSource} paramètre(s), Méthode destination : {_nbParamDestination} paramètre(s)" + Environment.NewLine + "La méthode destination peut comporter plus de paramètres que necessaire, mais pas moins";
            }
        }

        /// <summary>
        /// Nombre de paramètres de la méthode à remplacer
        /// </summary>
        public int NbParametresSource
        {
            get
            {
                return _nbParamSource;
            }
        }

        /// <summary>
        /// Nombre de paramètres de la méthode de remplacement
        /// </summary>
        public int NbParametresRemplacement
        {
            get
            {
                return _nbParamDestination;
            }
        }
    }
}
