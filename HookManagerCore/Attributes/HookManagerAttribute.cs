using System;

namespace HookManagerCore.Attributes
{
    /// <summary>
    /// Classe de base pour les attributs de la librairie HookManager
    /// </summary>
    public abstract class HookManagerAttribute : Attribute
    {
        /// <summary>
        /// Type contenant la méthode de remplacement
        /// </summary>
        public Type Classe { get; set; }
        /// <summary>
        /// Activer (ou non) automatiquement (au démarrage, si <see cref="HookPool.InitializeAllAttributeHook(bool)"/> est appelée)
        /// </summary>
        public bool AutoEnabled { get; set; } = true;
    }
}
