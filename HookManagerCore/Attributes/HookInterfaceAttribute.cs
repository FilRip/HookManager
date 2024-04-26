using System;

namespace HookManagerCore.Attributes
{
    /// <summary>
    /// Attributs pour remplacer les méthodes d'une interface (chez toutes les classes l'implémentant) par une autre automatiquement (au démarrage, si <see cref="HookPool.InitializeAllAttributeHook(bool)"/> est appelée)
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class HookInterfaceAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Nom de la méthode, si on ne veut qu'en remplacer qu'une
        /// </summary>
        public string NomMethode { get; set; }
    }
}
