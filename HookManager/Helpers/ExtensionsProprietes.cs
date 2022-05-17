using System;
using System.Diagnostics;
using System.Reflection;

namespace HookManager.Helpers
{
    /// <summary>
    /// Classes pour les méthodes d'extensionsde PropertyInfo
    /// </summary>
    public static class ExtensionsProprietes
    {
        /// <summary>
        /// Remplace le getter et/ou le setter de cette propriété
        /// </summary>
        /// <param name="propriete">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static void RemplacePar(this PropertyInfo propriete, string methodeGet, string methodeSet, bool autoActiver = true)
        {
            HookPool.GetInstance().AjouterProprieteRemplacement(propriete, methodeGet, methodeSet, autoActiver);
        }

        /// <summary>
        /// Remplace le getter et/ou le setter de cette propriété
        /// </summary>
        /// <param name="propriete">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static void RemplacePar(this PropertyInfo propriete, MethodInfo methodeGet, MethodInfo methodeSet, bool autoActiver = true)
        {
            HookPool.GetInstance().AjouterProprieteRemplacement(propriete, methodeGet, methodeSet, autoActiver);
        }

        /// <summary>
        /// Remplace le getter de cette propriété
        /// </summary>
        /// <param name="propriete">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static void RemplacerGetter(this PropertyInfo propriete, MethodInfo methodeGet, bool autoActiver = true)
        {
            RemplacePar(propriete, methodeGet, null, autoActiver);
        }

        /// <summary>
        /// Remplace le setter de cette propriété
        /// </summary>
        /// <param name="propriete">Propriété à remplacer</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static void RemplacerSetter(this PropertyInfo propriete, MethodInfo methodeSet, bool autoActiver = true)
        {
            RemplacePar(propriete, null, methodeSet, autoActiver);
        }

        /// <summary>
        /// Remplace le getter de cette propriété
        /// </summary>
        /// <param name="propriete">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static void RemplacerGetter(this PropertyInfo propriete, string methodeGet, bool autoActiver = true)
        {
            RemplacePar(propriete, methodeGet, "", autoActiver);
        }

        /// <summary>
        /// Remplace le setter de cette propriété
        /// </summary>
        /// <param name="propriete">Propriété à remplacer</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static void RemplacerSetter(this PropertyInfo propriete, string methodeSet, bool autoActiver = true)
        {
            RemplacePar(propriete, "", methodeSet, autoActiver);
        }

        /// <summary>
        /// Retourne le PropertyInfo d'une propriété d'une classe
        /// </summary>
        /// <param name="nomType">Nom de la classe/type contenant la propriété</param>
        /// <param name="nomPropriete">Nom de la propriété à rechercher</param>
        /// <param name="filtre">Filtre des propriétés</param>
        public static PropertyInfo GetPropertyInfo(string nomType, string nomPropriete, BindingFlags filtre)
        {
            return GetPropertyInfo(new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace, nomType, nomPropriete, filtre);
        }

        /// <summary>
        /// Retourne le PropertyInfo d'une propriété d'une classe
        /// </summary>
        /// <param name="nomNamespace">Nom du namespace ou se trouve la classe/le type</param>
        /// <param name="nomType">Nom de la classe/type contenant la propriété</param>
        /// <param name="nomPropriete">Nom de la propriété à rechercher</param>
        /// <param name="filtre">Filtre des propriétés</param>
        public static PropertyInfo GetPropertyInfo(string nomNamespace, string nomType, string nomPropriete, BindingFlags filtre)
        {
            if (string.IsNullOrWhiteSpace(nomType) || string.IsNullOrWhiteSpace(nomPropriete))
                return null;
            if (string.IsNullOrWhiteSpace(nomNamespace))
                nomNamespace = new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace;
            Type typeClasse;
            typeClasse = AppDomain.CurrentDomain.GetAssemblies().RechercheType(nomNamespace + "." + nomType);
            if (typeClasse == null)
                return null;
            PropertyInfo mi = null;
            try
            {
                mi = typeClasse.GetProperty(nomPropriete, filtre);
            }
            catch (Exception) { }
            return mi;
        }

        /// <summary>
        /// Retourne le PropertyInfo d'une propriété d'une classe
        /// </summary>
        /// <param name="nomNamespace">Nom du namespace ou se trouve la classe/le type</param>
        /// <param name="nomType">Nom de la classe/type contenant la propriété</param>
        /// <param name="nomPropriete">Nom de la propriété à rechercher</param>
        public static PropertyInfo GetPropertyInfo(string nomNamespace, string nomType, string nomPropriete)
        {
            if (string.IsNullOrWhiteSpace(nomNamespace))
                nomNamespace = new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace;
            return GetPropertyInfo(nomNamespace, nomType, nomPropriete, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Retourne le PropertyInfo d'une propriété d'une classe
        /// </summary>
        /// <param name="nomType">Nom de la classe/type contenant la propriété</param>
        /// <param name="nomPropriete">Nom de la propriété à rechercher</param>
        public static PropertyInfo GetPropertyInfo(string nomType, string nomPropriete)
        {
            return GetPropertyInfo(new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace, nomType, nomPropriete, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }
}
