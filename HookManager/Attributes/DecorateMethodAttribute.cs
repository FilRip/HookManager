using System;

namespace HookManager.Attributes
{
    /// <summary>
    /// Execute une méthode au début et/ou à la fin de cette méthode
    /// </summary>
    /// <remarks>Si vous décorez un constructeur : <br/>Il est fortement déconseillé d'initialiser des valeurs par défaut à des champs de la classe, elles seront écrasées par le constructeur.<br/>Mettez les valeurs par défaut des champs dans un constructeur, et non dans la déclaration du champs</remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class DecorateMethodAttribute : Attribute
    {
        /// <summary>
        /// Nom de la méthode (avec le type, et l'espace de nom au besoin) à appeler au début. Exemple : MonNameSpace.MaClasse.MaMethode (ou ignorer, vide, null si vous ne voulez pas exécuter de méthode avant celle-ci)
        /// </summary>
        public string MethodNameBefore { get; set; }
        /// <summary>
        /// Nom de la méthode (avec le type, et l'espace de nom au besoin) à appeler à la fin. Exemple : MonNameSpace.MaClasse.MaMethode (ou ignorer, vide, null si vous ne voulez pas exécuter de méthode après celle-ci)
        /// </summary>
        public string MethodNameAfter { get; set; }
        /// <summary>
        /// Activer (ou non) automatiquement (au démarrage, si <see cref="HookPool.InitializeAllAttributeHook(bool)"/> est appelée)
        /// </summary>
        public bool AutoEnabled { get; set; } = true;
    }
}
