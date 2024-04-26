using System;
using System.Runtime.Serialization;

namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Erreur tente de remplacer une méthode du GAC en mode Managé<br/>
    /// Il faut utiliser le "remplaceur de GAC" et non le "remplaceur" managé
    /// </summary>
    [Serializable()]
    public class CantHookGacException : HookManagerException
    {
        internal CantHookGacException() : base()
        {
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "Il n'est pas possible de substituer une méthode d'une classe inclus dans le GAC (GlobalAssemblyCache)" + Environment.NewLine + "Utilisez HookPool.getInstance().AjouterGACHook() pour substituer des méthodes du GAC";
            }
        }

        /// <inheritdoc/>
        protected CantHookGacException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
