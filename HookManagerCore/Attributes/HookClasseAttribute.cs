using System;

namespace HookManagerCore.Attributes
{
    /// <summary>
    /// Attribut pour remplacer, toutes ou partie, des méthodes de ce type de manière automatique (au démarrage, si <see cref="HookPool.InitializeAllAttributeHook(bool)"/> est appelée)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HookClasseAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Filtre les méthodes à remplacer
        /// </summary>
        public System.Reflection.BindingFlags FiltreMethode { get; set; }

        /// <summary>
        /// Filtre les méthodes à remplacer qui commence par le/les caractère(s) spécifié(s)
        /// </summary>
        public string PrefixNomMethode { get; set; }

        /// <summary>
        /// Filtre les méthodes à remplacer qui termine par le/les caractère(s) spécifié(s)
        /// </summary>
        public string SuffixeNomMethode { get; set; }
    }
}
