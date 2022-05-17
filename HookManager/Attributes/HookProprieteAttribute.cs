using System;

namespace HookManager.Attributes
{
    /// <summary>
    /// Attributs pour remplacer le getter et/ou le setter d'une propriété par une autre automatiquement (au démarrage, si <see cref="HookPool.PrepareMethodesTaggees(bool)"/> est appelée)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class HookProprieteAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Nom de la méthode pour le get de cette propriété
        /// </summary>
        public string GetMethode;
        /// <summary>
        /// Nom de la méthode pour le set de cette propriété
        /// </summary>
        public string SetMethode;
    }
}
