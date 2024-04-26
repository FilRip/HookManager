using System;

namespace HookManagerCore.Attributes
{
    /// <summary>
    /// Attributs pour remplacer ce constructeur par une autre automatiquement (au démarrage, si <see cref="HookPool.InitializeAllAttributeHook(bool)"/> est appelée)
    /// </summary>
    /// <remarks>Il est fortement déconseillé d'initialiser des valeurs par défaut à des champs de la classe, elles seront écrasées par le constructeur.<br/>Mettez les valeurs par défaut des champs dans un constructeur, et non dans la déclaration du champs</remarks>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public class HookConstructeurAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Nom de la méthode remplaçant ce constructeuri
        /// </summary>
        public string NomMethode { get; set; }
    }
}
