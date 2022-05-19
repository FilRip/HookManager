using System;

namespace HookManager.Attributes
{
    /// <summary>
    /// Classe de base pour les attributs de la librairie HookManager
    /// </summary>
    public abstract class HookManagerAttribute : Attribute
    {
        /// <summary>
        /// Type contenant la méthode de remplacement
        /// </summary>
        public Type Classe;
        /// <summary>
        /// Activer (ou non) automatiquement (au démarrage, si <see cref="HookPool.InitialiseTousHookParAttribut(bool)"/> est appelée)
        /// </summary>
        public bool AutoActiver = true;
    }
}
