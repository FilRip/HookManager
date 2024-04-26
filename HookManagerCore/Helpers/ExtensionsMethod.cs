using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using HookManagerCore.Exceptions;
using HookManagerCore.Modeles;

namespace HookManagerCore.Helpers
{
    /// <summary>
    /// Classe d'extensions pour les méthodes
    /// </summary>
    public static class ExtensionsMethod
    {
        internal static DynamicMethod GetOriginalMethod(this MethodInfo method)
        {
            return (DynamicMethod)HookPool.GetInstance().MyHarmony.Patch(method);
        }

        internal static void ReplaceManagedMethod(this MethodBase methodToReplace, MethodInfo methodToInject)
        {
            HookPool.GetInstance().MyHarmony.Patch(methodToReplace, new HarmonyLib.HarmonyMethod(methodToInject));
        }

        /// <summary>
        /// Faire une substitution d'une méthode managée à une autre méthode managée
        /// </summary>
        /// <param name="MethodeARemplacer">Nom de la méthode (avec le type, et l'espace de nom au besoin) à remplacer. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="MethodeDeRemplacement">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static ManagedHook ReplaceMethod(this MethodInfo MethodeARemplacer, string MethodeDeRemplacement, bool autoActiver = true)
        {
            return HookPool.GetInstance().AddHook(MethodeARemplacer.DeclaringType.Namespace + "." + MethodeARemplacer.DeclaringType.Name + "." + MethodeARemplacer.Name, MethodeDeRemplacement, autoActiver);
        }

        /// <summary>
        /// Faire une substitution d'une méthode managée à une autre méthode managée
        /// </summary>
        /// <param name="MethodeARemplacer">Nom de la méthode (avec le type, et l'espace de nom au besoin) à remplacer. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="MethodeDeRemplacement">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static ManagedHook ReplaceMethod(this MethodInfo MethodeARemplacer, MethodInfo MethodeDeRemplacement, bool autoActiver = true)
        {
            return HookPool.GetInstance().AddHook(MethodeARemplacer, MethodeDeRemplacement, autoActiver);
        }

        /// <summary>
        /// Retourne le MethodInfo d'une méthode d'une classe
        /// </summary>
        /// <param name="nomType">Nom de la classe/type contenant la méthode</param>
        /// <param name="nomMethode">Nom de la méthode à rechercher</param>
        /// <param name="filtre">Filtre des méthodes</param>
        public static MethodInfo GetMethodInfo(string nomType, string nomMethode, BindingFlags filtre)
        {
            return GetMethodInfo(new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace, nomType, nomMethode, filtre);
        }

        /// <summary>
        /// Retourne le MethodInfo d'une méthode d'une classe
        /// </summary>
        /// <param name="nomNamespace">Nom du namespace ou se trouve la classe/le type</param>
        /// <param name="nomType">Nom de la classe/type contenant la méthode</param>
        /// <param name="nomMethode">Nom de la méthode à rechercher</param>
        /// <param name="filtre">Filtre des méthodes</param>
        public static MethodInfo GetMethodInfo(string nomNamespace, string nomType, string nomMethode, BindingFlags filtre)
        {
            if (string.IsNullOrWhiteSpace(nomType) || string.IsNullOrWhiteSpace(nomMethode))
                return null;
            if (string.IsNullOrWhiteSpace(nomNamespace))
                nomNamespace = new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace;
            Type typeClasse;
            typeClasse = AppDomain.CurrentDomain.GetAssemblies().SearchType(nomNamespace + "." + nomType);
            if (typeClasse == null)
                return null;
            MethodInfo mi = null;
            try
            {
                mi = typeClasse.GetMethod(nomMethode, filtre);
            }
            catch (Exception)
            {
                // Ignore errors
            }
            return mi;
        }

        /// <summary>
        /// Retourne le MethodInfo d'une méthode d'une classe
        /// </summary>
        /// <param name="nomNamespace">Nom du namespace ou se trouve la classe/le type</param>
        /// <param name="nomType">Nom de la classe/type contenant la méthode</param>
        /// <param name="nomMethode">Nom de la méthode à rechercher</param>
        public static MethodInfo GetMethodInfo(string nomNamespace, string nomType, string nomMethode)
        {
            if (string.IsNullOrWhiteSpace(nomNamespace))
                nomNamespace = new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace;
            return GetMethodInfo(nomNamespace, nomType, nomMethode, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Retourne le MethodInfo d'une méthode d'une classe
        /// </summary>
        /// <param name="nomType">Nom de la classe/type contenant la méthode</param>
        /// <param name="nomMethode">Nom de la méthode à rechercher</param>
        public static MethodInfo GetMethodInfo(string nomType, string nomMethode)
        {
            return GetMethodInfo(new StackTrace().GetFrames()[1].GetMethod().DeclaringType.Namespace, nomType, nomMethode, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        internal static MethodInfo GetMethodByFullName(string methodName)
        {
            string className, methodNameSeparated;
            Type classSource;
            MethodInfo mi;

            if (methodName.IndexOf(".") >= 0)
            {
                className = methodName.Substring(0, methodName.LastIndexOf("."));
                classSource = AppDomain.CurrentDomain.GetAssemblies().SearchType(className);
                if (classSource == null)
                    throw new NoTypeInNameException(methodName);
                methodNameSeparated = methodName.Substring(methodName.LastIndexOf(".") + 1);
                mi = classSource.GetMethod(methodNameSeparated, HookPool.GetInstance().DefaultFilters);
                if (mi == null)
                    throw new TypeOrMethodNotFoundException(className, methodNameSeparated);
                return mi;
            }
            else
                throw new NoTypeInNameException(methodName);
        }

        internal static DynamicMethod CopyMethod(this MethodBase methodToCopy, string methodeName = "")
        {
            if (string.IsNullOrWhiteSpace(methodeName))
                methodeName = methodToCopy.Name + "_Copy";

            Type typeDeRetour = typeof(void);
            if (methodToCopy is MethodInfo mi)
                typeDeRetour = mi.ReturnType;

            int nbParametres = methodToCopy.GetParameters().Length;
            if (!methodToCopy.IsStatic || methodToCopy is ConstructorInfo)
                nbParametres += 1;
            Type[] listeParametres = new Type[nbParametres];
            if (!methodToCopy.IsStatic || methodToCopy is ConstructorInfo)
            {
                if (methodToCopy is ConstructorInfo ci)
                    listeParametres[0] = ci.DeclaringType;
                else
                    listeParametres[0] = typeDeRetour;
                nbParametres++;
            }

            foreach (ParameterInfo pi in methodToCopy.GetParameters())
                listeParametres[nbParametres++] = pi.ParameterType;

            // On copie le corps de la méthode
            List<ILCommande> listeOpCodes = methodToCopy.LireMethodBody();

            DynamicMethod methodeCopiee = new(methodeName, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeDeRetour, listeParametres, methodToCopy.DeclaringType, false);
            ILGenerator ilGen = methodeCopiee.GetILGenerator(methodToCopy.GetMethodBody().GetILAsByteArray().Length);

            // On copie ensuite les variables locale de la méthode
            if (methodToCopy.GetMethodBody().LocalVariables != null && methodToCopy.GetMethodBody().LocalVariables.Count > 0)
                foreach (LocalVariableInfo lv in methodToCopy.GetMethodBody().LocalVariables)
                    ilGen.DeclareLocal(lv.LocalType, lv.IsPinned);

            // On déclare enfin les labels
            int nbLabels = listeOpCodes.Count(cmd => cmd.debutLabel);
            if (nbLabels > 0)
                for (int i = 0; i < nbLabels; i++)
                    ilGen.DefineLabel();

            // Et on ajoute le corps dans notre méthode Dynamic
            foreach (ILCommande cmd in listeOpCodes)
            {
                cmd.Emit(ilGen);
            }

            return methodeCopiee;
        }

        internal static byte[] GetILAsByteArray(this DynamicMethod methode)
        {
            FieldInfo fiGen = typeof(DynamicMethod).GetField("m_ilGenerator", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo fiByteArray = typeof(ILGenerator).GetField("m_ILStream", BindingFlags.Instance | BindingFlags.NonPublic);
            ILGenerator ilgen = (ILGenerator)fiGen.GetValue(methode);
            return (byte[])fiByteArray.GetValue(ilgen);
        }
    }
}
