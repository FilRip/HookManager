using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using HookManager.Exceptions;
using HookManager.Modeles;

namespace HookManager.Helpers
{
    /// <summary>
    /// Classe d'extensions pour les méthodes
    /// </summary>
    public static class ExtensionsMethod
    {
        /// <summary>
        /// Return the pointer of a managed method in a 32 bits environment
        /// </summary>
        /// <param name="methode">Méthode pour laquelle on recherche son pointeur mémoire</param>
        internal static int GetMethodPointer_x86(this MethodBase methode)
        {
            if (methode.IsVirtual)
            {
                int index = (int)((Marshal.ReadInt64(new IntPtr(methode.MethodHandle.Value.ToInt32())) >> 32) & 0xFF);
                int classStart = methode.DeclaringType.TypeHandle.Value.ToInt32();
                classStart += 10 * IntPtr.Size;
                classStart = Marshal.ReadInt32(new IntPtr(classStart));
                classStart += index * IntPtr.Size;
                return classStart;
            }
            else
                return methode.MethodHandle.Value.ToInt32() + IntPtr.Size * 2;
        }

        /// <summary>
        /// Return the pointer of a managed method in a 64 bits environment
        /// </summary>
        /// <param name="methode">Méthode pour laquelle on recherche son pointeur mémoire</param>
        internal static long GetMethodPointer_x64(this MethodBase methode)
        {
            if (methode.IsVirtual)
            {
                int index = (int)((Marshal.ReadInt64(new IntPtr(methode.MethodHandle.Value.ToInt64())) >> 32) & 0xFF);
                long classStart = methode.DeclaringType.TypeHandle.Value.ToInt64();
                classStart += 8 * IntPtr.Size;
                classStart = Marshal.ReadInt64(new IntPtr(classStart));
                classStart += index * IntPtr.Size;
                return classStart;
            }
            else
                return methode.MethodHandle.Value.ToInt64() + IntPtr.Size;
        }

        internal static void ReplaceManagedMethod(this MethodBase methodToReplace, MethodBase methodToInject)
        {
            if (IntPtr.Size == 4)
            {
                int ptrInject, ptrCible;

                ptrCible = methodToReplace.GetMethodPointer_x86();
                ptrInject = methodToInject.GetMethodPointer_x86();

                if (Debugger.IsAttached)
                {
                    int injRead = Marshal.ReadInt32(new IntPtr(ptrInject));
                    int injOffset = injRead + 1;

                    int cibleRead = Marshal.ReadInt32(new IntPtr(ptrCible));
                    int cibleOffset = cibleRead + 1;

                    Marshal.WriteInt32(new(cibleOffset), injRead + 5 + Marshal.ReadInt32(new IntPtr(injOffset)) - (cibleRead + 5));
                }
                else
                    Marshal.WriteInt32(new IntPtr(ptrCible), Marshal.ReadInt32(new IntPtr(ptrInject)));
            }
            else
            {
                long ptrInject, ptrCible;

                ptrCible = methodToReplace.GetMethodPointer_x64();
                ptrInject = methodToInject.GetMethodPointer_x64();

                if (Debugger.IsAttached)
                {
                    long injRead = Marshal.ReadInt64(new IntPtr(ptrInject));
                    long injOffset = injRead + 1;

                    long cibleRead = Marshal.ReadInt64(new IntPtr(ptrCible));
                    long cibleOffset = cibleRead + 1;

                    // Le debugger VisualStudio est en 32bits, on écrit un pointeur 32 bits (malgré qu'on soit dans un environnement 64bits)
                    // Attention : VisualStudio 2022 (et +) sont en 64bits... possibilité de devoir modifier cela
                    Marshal.WriteInt32(new(cibleOffset), (int)injRead + 5 + Marshal.ReadInt32(new IntPtr(injOffset)) - ((int)cibleRead + 5));
                }
                else
                    Marshal.WriteInt64(new IntPtr(ptrCible), Marshal.ReadInt64(new IntPtr(ptrInject)));
            }
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
