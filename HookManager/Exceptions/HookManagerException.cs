using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Classe de base pour toutes erreurs de la librairie présente ; HookManager
    /// </summary>
    [Serializable()]
    public abstract class HookManagerException : Exception
    {
        internal HookManagerException() : base()
        {
        }
    }
}
