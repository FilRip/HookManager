using System;

namespace HookManager.Attributes
{
    /// <summary>
    /// Attribut pour remplacer, toutes ou partie, des méthodes de ce type de manière automatique (au démarrage, si <see cref="HookPool.InitialiseTousHookParAttribut(bool)"/> est appelée)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HookClasseAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Filtre les méthodes à remplacer
        /// </summary>
        public System.Reflection.BindingFlags filtreMethode;
        /// <summary>
        /// Filtre les méthodes à remplacer qui commence ou finit par le/les caractère(s) spécifié(s)
        /// </summary>
        public string prefixNomMethode, suffixeNomMethode;
    }
}
