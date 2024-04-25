using System;

namespace HookManager.Attributes
{
    /// <summary>
    /// Attributs pour remplacer cette méthode par une autre automatiquement (au démarrage, si <see cref="HookPool.InitializeAllAttributeHook(bool)"/> est appelée)<br/>
    /// Nb : L'ancienne devient inaccessible
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ReplaceMethodAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Nom de la méthode remplaçant celle-ci
        /// </summary>
        public string MethodName { get; set; }
    }
}
