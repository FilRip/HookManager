using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// Retourne le pointeur mémoire d'une méthode, dans un environement 32 bits
        /// </summary>
        /// <param name="methode">Méthode pour laquelle on recherche son pointeur mémoire</param>
        internal static int GetPointeurMethode_x86(this MethodBase methode)
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
        /// Retourne le pointeur mémoire d'une méthode, dans un environement 64 bits
        /// </summary>
        /// <param name="methode">Méthode pour laquelle on recherche son pointeur mémoire</param>
        internal static long GetPointeurMethode_x64(this MethodBase methode)
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

        internal static void RemplaceMethodeManagee(this MethodBase methodToReplace, MethodBase methodToInject)
        {
            if (IntPtr.Size == 4)
            {
                int ptrInject, ptrCible;

                ptrCible = methodToReplace.GetPointeurMethode_x86();
                ptrInject = methodToInject.GetPointeurMethode_x86();

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

                ptrCible = methodToReplace.GetPointeurMethode_x64();
                ptrInject = methodToInject.GetPointeurMethode_x64();

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
        public static ManagedHook RemplaceMethode(this MethodInfo MethodeARemplacer, string MethodeDeRemplacement, bool autoActiver = true)
        {
            return HookPool.GetInstance().AjouterHook(MethodeARemplacer.DeclaringType.Namespace + "." + MethodeARemplacer.DeclaringType.Name + "." + MethodeARemplacer.Name, MethodeDeRemplacement, autoActiver);
        }

        /// <summary>
        /// Faire une substitution d'une méthode managée à une autre méthode managée
        /// </summary>
        /// <param name="MethodeARemplacer">Nom de la méthode (avec le type, et l'espace de nom au besoin) à remplacer. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="MethodeDeRemplacement">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public static ManagedHook RemplaceMethode(this MethodInfo MethodeARemplacer, MethodInfo MethodeDeRemplacement, bool autoActiver = true)
        {
            return HookPool.GetInstance().AjouterHook(MethodeARemplacer, MethodeDeRemplacement, autoActiver);
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
            typeClasse = AppDomain.CurrentDomain.GetAssemblies().RechercheType(nomNamespace + "." + nomType);
            if (typeClasse == null)
                return null;
            MethodInfo mi = null;
            try
            {
                mi = typeClasse.GetMethod(nomMethode, filtre);
            }
            catch (Exception) { }
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

        internal static MethodInfo RetourneMethodeParLeNomComplet(string nomMethode)
        {
            string nomClasse, nomMethodeSplittee;
            Type classeSource;
            MethodInfo mi;

            if (nomMethode.IndexOf(".") > 0)
            {
                nomClasse = nomMethode.Substring(0, nomMethode.LastIndexOf("."));
                classeSource = AppDomain.CurrentDomain.GetAssemblies().RechercheType(nomClasse);
                if (classeSource == null)
                    throw new NoTypeInName(nomMethode);
                nomMethodeSplittee = nomMethode.Substring(nomMethode.LastIndexOf(".") + 1);
                mi = classeSource.GetMethod(nomMethodeSplittee, HookPool.GetInstance().FiltresParDefaut);
                if (mi == null)
                    throw new TypeOrMethodNotFound(nomClasse, nomMethodeSplittee);
                return mi;
            }
            else
                throw new NoTypeInName(nomMethode);
        }

        internal static DynamicMethod CopierMethode(this MethodBase methodeACopier, string nomMethode = "")
        {
            if (string.IsNullOrWhiteSpace(nomMethode))
                nomMethode = methodeACopier.Name + "_Copy";

            Type typeDeRetour = typeof(void);
            if (methodeACopier is ConstructorInfo ci)
                typeDeRetour = ci.DeclaringType;
            else if (methodeACopier is MethodInfo mi)
                typeDeRetour = mi.ReturnType;

            int nbParametres = methodeACopier.GetParameters().Length;
            if (!methodeACopier.IsStatic || methodeACopier is ConstructorInfo)
                nbParametres += 1;
            Type[] listeParametres = new Type[nbParametres];
            if (!methodeACopier.IsStatic || methodeACopier is ConstructorInfo)
            {
                listeParametres[0] = typeDeRetour;
                nbParametres++;
            }
            foreach (ParameterInfo pi in methodeACopier.GetParameters())
                listeParametres[nbParametres++] = pi.ParameterType;

            DynamicMethod methodeCopiee = new(nomMethode, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeDeRetour, listeParametres, methodeACopier.DeclaringType, false);
            ILGenerator ilGen = methodeCopiee.GetILGenerator(methodeACopier.GetMethodBody().GetILAsByteArray().Length);

            // On copie déjà les variables locale de la méthode
            if (methodeACopier.GetMethodBody().LocalVariables != null && methodeACopier.GetMethodBody().LocalVariables.Count > 0)
                foreach (LocalVariableInfo lv in methodeACopier.GetMethodBody().LocalVariables)
                    ilGen.DeclareLocal(lv.LocalType, lv.IsPinned);

            // On copie ensuite le corps de la méthode
            List<ILCommande> listeOpCodes = methodeACopier.LireMethodBody();
            foreach (ILCommande cmd in listeOpCodes)
            {
                cmd.Emit(ilGen);
            }

            // On ajoute le "return <value>" si c'est une méthode qui retourne quelque chose (pas une void) et que le return n'est pas présent (dans le cas d'un constructeur, il n'est pas présent)
            if (typeDeRetour != typeof(void) && listeOpCodes[listeOpCodes.Count - 1].CodeIL.Value != OpCodes.Ret.Value)
            {
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Ret);
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
