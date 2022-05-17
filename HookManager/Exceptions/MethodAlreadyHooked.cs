using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, la méthode que vous tentez de remplacer est déjà remplacée, consultez HookPool pour la liste
    /// </summary>
    [Serializable()]
    public class MethodAlreadyHooked : HookManagerException
    {
        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "Cette méthode est déjà crochetée. On ne peut crocheter qu'une seule fois chaque méthode.";
            }
        }
    }
}
