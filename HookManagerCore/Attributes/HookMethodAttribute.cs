using System;

namespace HookManagerCore.Attributes
{
    /// <summary>
    /// Attributs pour remplacer cette méthode par une autre automatiquement (au démarrage, si <see cref="HookPool.InitializeAllAttributeHook(bool)"/> est appelée)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class HookMethodAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Nom de la méthode remplaçant celle-ci
        /// </summary>
        public string MethodName { get; set; }
    }
}
