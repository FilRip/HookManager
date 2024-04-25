using System;
using System.Diagnostics;
using System.Reflection;

namespace HookManager.Helpers
{
    /// <summary>
    /// Classes pour les méthodes d'extensionsde PropertyInfo
    /// </summary>
    public static class ExtensionsProperties
    {
        /// <summary>
        /// Remplace le getter et/ou le setter de cette propriété
        /// </summary>
        /// <param name="property">Propriété à remplacer</param>
        /// <param name="methodGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public static void ReplaceBy(this PropertyInfo property, string methodGet, string methodSet, bool autoEnable = true)
        {
            HookPool.GetInstance().AddPropertyReplacement(property, methodGet, methodSet, autoEnable);
        }

        /// <summary>
        /// Remplace le getter et/ou le setter de cette propriété
        /// </summary>
        /// <param name="property">Propriété à remplacer</param>
        /// <param name="methodGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public static void ReplaceBy(this PropertyInfo property, MethodInfo methodGet, MethodInfo methodSet, bool autoEnable = true)
        {
            HookPool.GetInstance().AddPropertyReplacement(property, methodGet, methodSet, autoEnable);
        }

        /// <summary>
        /// Remplace le getter de cette propriété
        /// </summary>
        /// <param name="property">Propriété à remplacer</param>
        /// <param name="methodGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public static void ReplaceGetter(this PropertyInfo property, MethodInfo methodGet, bool autoEnable = true)
        {
            ReplaceBy(property, methodGet, null, autoEnable);
        }

        /// <summary>
        /// Remplace le getter de cette propriété
        /// </summary>
        /// <param name="property">Propriété à remplacer</param>
        /// <param name="methodGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public static void ReplaceGetter(this PropertyInfo property, string methodGet, bool autoEnable = true)
        {
            ReplaceBy(property, methodGet, "", autoEnable);
        }

        /// <summary>
        /// Remplace le setter de cette propriété
        /// </summary>
        /// <param name="property">Propriété à remplacer</param>
        /// <param name="methodSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public static void ReplaceSetter(this PropertyInfo property, MethodInfo methodSet, bool autoEnable = true)
        {
            ReplaceBy(property, null, methodSet, autoEnable);
        }

        /// <summary>
        /// Remplace le setter de cette propriété
        /// </summary>
        /// <param name="property">Propriété à remplacer</param>
        /// <param name="methodSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public static void ReplaceSetter(this PropertyInfo property, string methodSet, bool autoEnable = true)
        {
            ReplaceBy(property, "", methodSet, autoEnable);
        }

        /// <summary>
        /// Retourne le PropertyInfo d'une propriété d'une classe
        /// </summary>
        /// <param name="typeName">Nom de la classe/type contenant la propriété</param>
        /// <param name="propertyName">Nom de la propriété à rechercher</param>
        /// <param name="filters">Filtre des propriétés</param>
        public static PropertyInfo GetPropertyInfo(string typeName, string propertyName, BindingFlags filters)
        {
            return GetPropertyInfo(new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace, typeName, propertyName, filters);
        }

        /// <summary>
        /// Retourne le PropertyInfo d'une propriété d'une classe
        /// </summary>
        /// <param name="namespaceName">Nom du namespace ou se trouve la classe/le type</param>
        /// <param name="typeName">Nom de la classe/type contenant la propriété</param>
        /// <param name="propertyName">Nom de la propriété à rechercher</param>
        /// <param name="filters">Filtre des propriétés</param>
        public static PropertyInfo GetPropertyInfo(string namespaceName, string typeName, string propertyName, BindingFlags filters)
        {
            if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(propertyName))
                return null;
            if (string.IsNullOrWhiteSpace(namespaceName))
                namespaceName = new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace;
            Type typeClasse;
            typeClasse = AppDomain.CurrentDomain.GetAssemblies().SearchType(namespaceName + "." + typeName);
            if (typeClasse == null)
                return null;
            PropertyInfo mi = null;
            try
            {
                mi = typeClasse.GetProperty(propertyName, filters);
            }
            catch (Exception)
            {
                // Ignore errors
            }
            return mi;
        }

        /// <summary>
        /// Retourne le PropertyInfo d'une propriété d'une classe
        /// </summary>
        /// <param name="namespaceName">Nom du namespace ou se trouve la classe/le type</param>
        /// <param name="typeName">Nom de la classe/type contenant la propriété</param>
        /// <param name="propertyName">Nom de la propriété à rechercher</param>
        public static PropertyInfo GetPropertyInfo(string namespaceName, string typeName, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                namespaceName = new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace;
            return GetPropertyInfo(namespaceName, typeName, propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Retourne le PropertyInfo d'une propriété d'une classe
        /// </summary>
        /// <param name="typeName">Nom de la classe/type contenant la propriété</param>
        /// <param name="propertyName">Nom de la propriété à rechercher</param>
        public static PropertyInfo GetPropertyInfo(string typeName, string propertyName)
        {
            return GetPropertyInfo(new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace, typeName, propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
