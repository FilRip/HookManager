using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur tente de remplacer une méthode du GAC en mode Managé<br/>
    /// Il faut utiliser le "remplaceur de GAC" et non le "remplaceur" managé
    /// </summary>
    [Serializable()]
    public class CantHookGAC : HookManagerException
    {
        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "Il n'est pas possible de substituer une méthode d'une classe inclus dans le GAC (GlobalAssemblyCache)" + Environment.NewLine + "Utilisez HookPool.getInstance().AjouterGACHook() pour substituer des méthodes du GAC";
            }
        }
    }
}
