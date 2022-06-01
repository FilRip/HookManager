using System;

namespace HookManager.Attributes
{
    /// <summary>
    /// Attributs pour remplacer ce constructeur par une autre automatiquement (au démarrage, si <see cref="HookPool.InitialiseTousHookParAttribut(bool)"/> est appelée)
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public class HookConstructeurAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Nom de la méthode remplaçant ce constructeuri
        /// </summary>
        public string NomMethode;
    }
}
