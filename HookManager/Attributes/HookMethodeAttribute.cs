using System;

namespace HookManager.Attributes
{
    /// <summary>
    /// Attributs pour remplacer cette méthode par une autre automatiquement (au démarrage, si <see cref="HookPool.InitialiseTousHookParAttribut(bool)"/> est appelée)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class HookMethodeAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Nom de la méthode remplaçant celle-ci
        /// </summary>
        public string NomMethode;
    }
}
