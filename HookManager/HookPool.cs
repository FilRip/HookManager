using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using HookManager.Attributes;
using HookManager.Exceptions;
using HookManager.Helpers;
using HookManager.Modeles;

using Microsoft.CSharp;

namespace HookManager
{
    /// <summary>
    /// Classe principale de gestion des remplacements de méthodes par une autre
    /// </summary>
    public sealed class HookPool
    {
        private Dictionary<uint, ManagedHook> _listeHook;
        private Dictionary<MethodInfo, GACHook> _listeGACHook;
        private Dictionary<NativeMethod, NativeHook> _listeNativeHook;
        private Dictionary<uint, MethodeRemplacementHook> _listeRemplacement;
        private Dictionary<uint, ManagedHook> _listeDecoration;

        private readonly object _lockNumHook = new();

        private uint _nbHook;
        private static HookPool _instance;
        private BindingFlags _filtres;
        private bool _modeDebugInterne = false;

        private CSharpCodeProvider _compilateur;

        internal const string NOM_CLASSE = "HookClasse_";
        internal const string NOM_METHODE = "HookMethode_";
        internal const string NOM_METHODE_PARENTE = "InvokeParente_";

        internal HookPool()
        {
        }

        /// <summary>
        /// Retourne l'instance unique de cette classe
        /// </summary>
        public static HookPool GetInstance()
        {
            if (_instance == null)
            {
                _instance = new HookPool();
                _instance.InitSingleton();
            }
            return _instance;
        }

        private void InitSingleton()
        {
            _listeHook = new();
            _listeGACHook = new();
            _listeNativeHook = new();
            _listeRemplacement = new();
            _listeDecoration = new();

            _nbHook = 0;
            _filtres = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        }

        /// <summary>
        /// Permet de rentrer en pas à pas, en debuggage, dans les méthodes gérant les Hook lorsque l'on demande à faire du pas à pas dans une méthode "hookée"<br/>
        /// Activer le mode debug pas à pas dans les fichiers générés pendant l'exécution contenant les "fausses" méthodes gérant le remplacement.<br/>
        /// Ralentit le système, à n'utiliser que pour débogger la librairie HookManager (vous pouvez toujours, même sans ca, débogger du code remplacé, ca n'a pas de rapport)<br/>
        /// Utilise le répertoire TEMP (définit par la variable d'environement) pour y stocker les fichiers générés
        /// </summary>
        public bool ModeDebugInterne
        {
            get { return _modeDebugInterne; }
            set { _modeDebugInterne = value; }
        }

        /// <summary>
        /// Filtres par défaut pour la recherche des méthodes managées
        /// </summary>
        public BindingFlags FiltresParDefaut
        {
            get { return _filtres; }
            set { _filtres = value; }
        }

        /// <summary>
        /// Remplace le getter et/ou le setter d'une propriété
        /// </summary>
        /// <param name="proprieteFrom">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterProprieteRemplacement(PropertyInfo proprieteFrom, MethodInfo methodeGet, MethodInfo methodeSet, bool autoActiver = true)
        {
            if (proprieteFrom == null)
                throw new ArgumentNullException(nameof(proprieteFrom));
            if ((methodeGet == null) && (methodeSet == null))
                throw new ProprieteHookException(proprieteFrom.Name, "", "", CODE_ERREUR_PROPRIETE_HOOK.Remplacement_inutile);
            if (methodeGet != null)
            {
                if (proprieteFrom.GetGetMethod() == null)
                    throw new ProprieteHookException(proprieteFrom.Name, methodeGet.Name, "", CODE_ERREUR_PROPRIETE_HOOK.Pas_de_get);
                AjouterHook(proprieteFrom.GetGetMethod(), methodeGet, autoActiver);
            }
            if (methodeSet != null)
            {
                if (proprieteFrom.GetSetMethod() == null)
                    throw new ProprieteHookException(proprieteFrom.Name, "", methodeSet.Name, CODE_ERREUR_PROPRIETE_HOOK.Pas_de_set);
                AjouterHook(proprieteFrom.GetSetMethod(), methodeSet, autoActiver);
            }
        }

        /// <summary>
        /// Remplace le getter et/ou le setter d'une propriété
        /// </summary>
        /// <param name="proprieteFrom">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterProprieteRemplacement(PropertyInfo proprieteFrom, string methodeGet, string methodeSet, bool autoActiver = true)
        {
            if (proprieteFrom == null)
                throw new ArgumentNullException(nameof(proprieteFrom));
            if (string.IsNullOrWhiteSpace(methodeGet) && string.IsNullOrWhiteSpace(methodeSet))
                throw new ProprieteHookException(proprieteFrom.Name, "", "", CODE_ERREUR_PROPRIETE_HOOK.Remplacement_inutile);
            MethodInfo miGet = null, miSet = null;
            if (!string.IsNullOrWhiteSpace(methodeGet))
                miGet = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeGet);
            if (!string.IsNullOrWhiteSpace(methodeSet))
                miSet = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeSet);

            AjouterProprieteRemplacement(proprieteFrom, miGet, miSet, autoActiver);
        }

        /// <summary>
        /// Remplace le getter d'une propriété
        /// </summary>
        /// <param name="proprieteFrom">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterProprieteGetterRemplacement(string proprieteFrom, string methodeGet, bool autoActiver = true)
        {
            AjouterProprieteRemplacement(proprieteFrom, methodeGet, "", autoActiver);
        }

        /// <summary>
        /// Remplace le getter d'une propriété
        /// </summary>
        /// <param name="proprieteFrom">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterProprieteGetterRemplacement(PropertyInfo proprieteFrom, MethodInfo methodeGet, bool autoActiver = true)
        {
            AjouterProprieteRemplacement(proprieteFrom, methodeGet, null, autoActiver);
        }

        /// <summary>
        /// Remplace le setter d'une propriété
        /// </summary>
        /// <param name="proprieteFrom">Propriété à remplacer</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterProprieteSetterRemplacement(string proprieteFrom, string methodeSet, bool autoActiver = true)
        {
            AjouterProprieteRemplacement(proprieteFrom, "", methodeSet, autoActiver);
        }

        /// <summary>
        /// Remplace le setter d'une propriété
        /// </summary>
        /// <param name="proprieteFrom">Propriété à remplacer</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterProprieteSetterRemplacement(PropertyInfo proprieteFrom, MethodInfo methodeSet, bool autoActiver = true)
        {
            AjouterProprieteRemplacement(proprieteFrom, null, methodeSet, autoActiver);
        }

        /// <summary>
        /// Remplace le getter et/ou le setter d'une propriété
        /// </summary>
        /// <param name="proprieteFrom">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterProprieteRemplacement(string proprieteFrom, string methodeGet, string methodeSet, bool autoActiver = true)
        {
            if (string.IsNullOrWhiteSpace(proprieteFrom))
                throw new ArgumentNullException(nameof(proprieteFrom));
            if (string.IsNullOrWhiteSpace(methodeGet) && string.IsNullOrWhiteSpace(methodeSet))
                throw new ProprieteHookException(proprieteFrom, "", "", CODE_ERREUR_PROPRIETE_HOOK.Remplacement_inutile);
            PropertyInfo piSource = null;
            if (proprieteFrom.IndexOf(".") > 0)
            {
                string nomClasse = proprieteFrom.Substring(0, proprieteFrom.LastIndexOf("."));
                Type classeSource = AppDomain.CurrentDomain.GetAssemblies().RechercheType(nomClasse);
                if (classeSource == null)
                    throw new NoTypeInName(proprieteFrom);
                string prop = proprieteFrom.Substring(proprieteFrom.LastIndexOf(".") + 1);
                piSource = classeSource.GetProperty(prop, _filtres);
            }

            if (piSource == null)
                throw new ProprieteHookException(proprieteFrom, "", "", CODE_ERREUR_PROPRIETE_HOOK.Propriete_introuvable);

            MethodInfo miGet = null, miSet = null;
            if (!string.IsNullOrWhiteSpace(methodeGet))
                miGet = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeGet);
            if (!string.IsNullOrWhiteSpace(methodeSet))
                miSet = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeSet);

            AjouterProprieteRemplacement(piSource, miGet, miSet, autoActiver);
        }

        /// <summary>
        /// Ajouter une décoration à une méthode<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après celle mentionnée.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="methodeFrom">Méthode à décorer</param>
        /// <param name="methodeAvant">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodeApres">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public ManagedHook AjouterDecoration(string methodeFrom, string methodeAvant, string methodeApres, bool autoActiver = true)
        {
            if (string.IsNullOrWhiteSpace(methodeAvant) && string.IsNullOrWhiteSpace(methodeApres))
                throw new DecorationMethodesException(methodeAvant, methodeApres);
            MethodInfo miSource, miAvant = null, miApres = null;
            miSource = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeFrom);
            if (!string.IsNullOrWhiteSpace(methodeAvant))
                miAvant = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeAvant);
            if (!string.IsNullOrWhiteSpace(methodeApres))
                miApres = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeApres);

            return AjouterDecoration(miSource, miAvant, miApres, autoActiver);
        }

        /// <summary>
        /// Ajouter une décoration à tout une liste méthode<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après celle mentionnée.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="methodeFrom">Liste des méthodes à décorer</param>
        /// <param name="methodeAvant">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodeApres">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterDecoration(string[] methodeFrom, string methodeAvant, string methodeApres, bool autoActiver = true)
        {
            if (methodeFrom == null || methodeFrom.Length == 0)
                throw new ArgumentNullException(nameof(methodeFrom));

            if (string.IsNullOrWhiteSpace(methodeAvant) && string.IsNullOrWhiteSpace(methodeApres))
                throw new DecorationMethodesException(methodeAvant, methodeApres);
            MethodInfo miSource, miAvant = null, miApres = null;
            if (!string.IsNullOrWhiteSpace(methodeAvant))
                miAvant = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeAvant);
            if (!string.IsNullOrWhiteSpace(methodeApres))
                miApres = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeApres);

            foreach (string methodeSource in methodeFrom)
            {
                miSource = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeSource);
                AjouterDecoration(miSource, miAvant, miApres, autoActiver);
            }
        }

        /// <summary>
        /// Ajouter une décoration à tout une liste méthode<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après celle mentionnée.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="methodeFrom">Liste des méthodes à décorer</param>
        /// <param name="methodeAvant">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodeApres">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public void AjouterDecoration(MethodInfo[] methodeFrom, MethodInfo methodeAvant, MethodInfo methodeApres, bool autoActiver = true)
        {
            if (methodeFrom == null || methodeFrom.Length == 0)
                throw new ArgumentNullException(nameof(methodeFrom));
            foreach (MethodInfo mi in methodeFrom)
                AjouterDecoration(mi, methodeAvant, methodeApres, autoActiver);
        }

        /// <summary>
        /// Ajouter une décoration à une méthode<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après celle mentionnée.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="methodeFrom">Méthode à décorer</param>
        /// <param name="methodeAvant">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodeApres">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public ManagedHook AjouterDecoration(MethodInfo methodeFrom, MethodInfo methodeAvant, MethodInfo methodeApres, bool autoActiver = true)
        {
            if (methodeFrom == null)
                throw new ArgumentNullException(nameof(methodeFrom));
            if (methodeFrom.DeclaringType?.Assembly == Assembly.GetExecutingAssembly())
                throw new DoNotHookMyLib();
            if (methodeAvant == null && methodeApres == null)
                throw new DecorationMethodesException(methodeAvant?.Name, methodeApres?.Name);
            if (MethodeDejaDecoree(methodeFrom))
                throw new MethodeAlreadyDecorated(methodeFrom.Name);
            if (methodeAvant != null)
                VerifierCorrespondances(methodeFrom, methodeAvant);
            if (methodeApres != null)
            {
                int nbParam = 0;
                if (methodeFrom.IsStatic)
                    nbParam++;
                if (methodeFrom.ReturnType != typeof(void))
                    nbParam++;
                if (methodeApres.GetParameters().Length < nbParam)
                    throw new MissingDefaultArgument(methodeApres.Name);
            }

            uint numHook = IncrNumHook();
            ManagedHook mh = new(numHook, methodeFrom, methodeAvant, methodeApres, autoActiver);
            if (mh != null)
                _listeHook.Add(numHook, mh);
            return mh;
        }

        /// <summary>
        /// Faire une substitution d'une méthode managée à une autre méthode managée
        /// </summary>
        /// <param name="methodeFrom">Méthode managée à remplacer</param>
        /// <param name="methodeTo">Méthode managée appelée à la place</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public ManagedHook AjouterHook(MethodInfo methodeFrom, MethodInfo methodeTo, bool autoActiver = true)
        {
            VerificationCommunes(methodeFrom, methodeTo);
            VerifierCorrespondances(methodeFrom, methodeTo);
            uint numHook = IncrNumHook();
            ManagedHook hook = new(numHook, methodeFrom, methodeTo, autoActiver);
            if (hook != null)
                _listeHook.Add(numHook, hook);
            return hook;
        }

        /// <summary>
        /// Faire une substitution d'un constructeur vers une autre méthode managée
        /// </summary>
        /// <param name="constructeurARemplacer">Constructeur à remplacer</param>
        /// <param name="methodeTo">Méthode remplacant le constructeur</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public ManagedHook AjouterHook(ConstructorInfo constructeurARemplacer, MethodInfo methodeTo, bool autoActiver = true)
        {
            if (constructeurARemplacer == null)
                throw new ArgumentNullException(nameof(constructeurARemplacer));
            VerificationCommunes(constructeurARemplacer, methodeTo);
            VerifierCorrespondances(constructeurARemplacer, methodeTo);
            uint numHook = IncrNumHook();
            ManagedHook hook = new(numHook, constructeurARemplacer, methodeTo, autoActiver);
            if (hook != null)
                _listeHook.Add(numHook, hook);
            return hook;
        }

        /// <summary>
        /// Ajouter une décoration à un constructeur<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après le constructeur mentionné.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="constructeurADecorer">Constructeur à décorer</param>
        /// <param name="methodeAvant">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodeApres">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoActiver">Active ou non tout de suite la décoration</param>
        public ManagedHook AjouterDecorationConstructeur(ConstructorInfo constructeurADecorer, MethodInfo methodeAvant, MethodInfo methodeApres, bool autoActiver = true)
        {
            if (constructeurADecorer == null)
                throw new ArgumentNullException(nameof(constructeurADecorer));
            if (constructeurADecorer.DeclaringType?.Assembly == Assembly.GetExecutingAssembly())
                throw new DoNotHookMyLib();
            if (methodeAvant == null && methodeApres == null)
                throw new DecorationMethodesException(methodeAvant?.Name, methodeApres?.Name);
            if (MethodeDejaDecoree(constructeurADecorer))
                throw new MethodeAlreadyDecorated(constructeurADecorer.Name);
            if (methodeAvant != null)
                VerifierCorrespondances(constructeurADecorer, methodeAvant);
            if (methodeApres != null)
            {
                int nbParam = 1;
                if (constructeurADecorer.IsStatic)
                    nbParam++;
                if (methodeApres.GetParameters().Length < nbParam)
                    throw new MissingDefaultArgument(methodeApres.Name);
            }

            uint numHook = IncrNumHook();
            ManagedHook mh = new(numHook, constructeurADecorer, methodeAvant, methodeApres, autoActiver);
            if (mh != null)
                _listeHook.Add(numHook, mh);
            return mh;
        }

        /// <summary>
        /// Faire une substitution d'une méthode managée à une autre méthode managée
        /// </summary>
        /// <param name="MethodeARemplacer">Nom de la méthode (avec le type, et l'espace de nom au besoin) à remplacer. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="MethodeDeRemplacement">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public ManagedHook AjouterHook(string MethodeARemplacer, string MethodeDeRemplacement, bool autoActiver = true)
        {
            MethodInfo miSource, miDest;

            miSource = ExtensionsMethod.RetourneMethodeParLeNomComplet(MethodeARemplacer);
            miDest = ExtensionsMethod.RetourneMethodeParLeNomComplet(MethodeDeRemplacement);

            return AjouterHook(miSource, miDest, autoActiver);
        }

        private void VerificationCommunes(MethodBase methodeFrom, MethodInfo methodeTo, bool testMethodeFrom = true, bool testMethodeTo = true)
        {
            if ((methodeFrom == null) && (testMethodeFrom))
                throw new ArgumentNullException(nameof(methodeFrom));
            if ((methodeTo == null) && (testMethodeTo))
                throw new ArgumentNullException(nameof(methodeTo));
            if (methodeFrom.DeclaringType == null)
                throw new CantHookDynamicMethod(methodeFrom.Name);
            if (methodeFrom.DeclaringType.Assembly.IsJITOptimizerEnabled() && Debugger.IsAttached)
                throw new CantHookJITOptimized(methodeFrom.DeclaringType.Assembly?.GetName()?.Name);
            if (methodeFrom.DeclaringType.Assembly.IsJITOptimizerEnabled() && methodeFrom is MethodInfo miFrom && miFrom.ReturnType != typeof(void))
                throw new CantHookJITOptimized(methodeFrom.DeclaringType.Assembly?.GetName()?.Name, methodeFrom.Name);
            if (methodeFrom.DeclaringType.Assembly.IsJITOptimizerEnabled() && methodeFrom.IsConstructor)
                throw new CantHookJITOptimized(methodeFrom.DeclaringType.Assembly?.GetName()?.Name, methodeFrom.Name);
            if (methodeFrom.DeclaringType.Assembly == Assembly.GetExecutingAssembly())
                throw new DoNotHookMyLib();
            if (MethodeDejaRemplacee(methodeFrom))
                throw new MethodAlreadyHooked(methodeFrom.Name);
            if (methodeFrom.DeclaringType.Assembly.GlobalAssemblyCache)
                throw new CantHookGAC();
            ProcessorArchitecture paFrom, paTo;
            if (testMethodeFrom && testMethodeTo)
            {
                paFrom = methodeFrom.DeclaringType.Assembly.GetName().ProcessorArchitecture;
                paTo = methodeTo.DeclaringType.Assembly.GetName().ProcessorArchitecture;
                if ((paFrom == ProcessorArchitecture.X86 && (paTo == ProcessorArchitecture.Amd64 || paTo == ProcessorArchitecture.IA64)) ||
                    ((paFrom == ProcessorArchitecture.Amd64 || paFrom == ProcessorArchitecture.IA64) && paTo == ProcessorArchitecture.X86))
                    throw new AssemblyPlatformeDifferente(paFrom, paTo);
                if (paFrom == ProcessorArchitecture.Arm || paTo == ProcessorArchitecture.Arm)
                    throw new PlateformeNonSupportee(ProcessorArchitecture.Arm);
            }
        }

        /// <summary>
        /// Remplace une méthode par une autre (l'ancienne devient inaccessible)
        /// </summary>
        /// <param name="methodeFrom">Méthode managée à remplacer</param>
        /// <param name="methodeTo">Méthode managée appelée à la place</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public MethodeRemplacementHook AjouterRemplacement(MethodInfo methodeFrom, MethodInfo methodeTo, bool autoActiver = true)
        {
            VerificationCommunes(methodeFrom, methodeTo);
            VerifierCorrespondances(methodeFrom, methodeTo);
            uint numHook = IncrNumHook();
            MethodeRemplacementHook hook = new(numHook, methodeFrom, methodeTo, autoActiver);
            if (hook != null)
                _listeRemplacement.Add(numHook, hook);
            return hook;
        }

        /// <summary>
        /// Remplace une méthode par une autre (l'ancienne devient inaccessible)
        /// </summary>
        /// <param name="methodeFrom">Nom de la méthode (avec le type, et l'espace de nom au besoin) à remplacer. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="methodeTo">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        public MethodeRemplacementHook AjouterRemplacement(string methodeFrom, string methodeTo, bool autoActiver = true)
        {
            MethodInfo miFrom, miTo;

            miFrom = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeFrom);
            miTo = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodeTo);

            return AjouterRemplacement(miFrom, miTo, autoActiver);
        }

        internal uint IncrNumHook()
        {
            lock (_lockNumHook)
                return ++_nbHook;
        }

        /// <summary>
        /// Faire une substitution d'une méthode Native (non managée) à une méthode managée
        /// </summary>
        /// <param name="methodFrom">Méthode native à remplacer</param>
        /// <param name="methodTo">Méthode managée appelée à la place</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public NativeHook AjouterNativeHook(NativeMethod methodFrom, MethodInfo methodTo, bool autoActiver = true)
        {
            if (methodFrom == null)
                throw new ArgumentNullException(nameof(methodFrom));
            if (methodFrom.ModuleName == Assembly.GetExecutingAssembly().GetName()?.Name + ".dll")
                throw new DoNotHookMyLib();
            if (_listeNativeHook.Keys.Any(hook => hook.ModuleName == methodFrom.ModuleName && hook.Method == methodFrom.Method))
                throw new MethodAlreadyHooked(methodFrom.Method);

            NativeHook natHook = new(methodFrom, methodTo);
            if (natHook != null)
            {
                if (autoActiver) natHook.Apply();
                _listeNativeHook.Add(methodFrom, natHook);
            }
            return natHook;
        }

        /// <summary>
        /// Faire une substitution d'une méthode Native (non managée) à une méthode managée
        /// </summary>
        /// <param name="nomLibrairie">Nom de la librairie contenant la méthode à remplacer (Exemple "user32.dll")</param>
        /// <param name="nomMethode">Nom de la méthode non managée, de la librairie, à remplacer</param>
        /// <param name="methodTo">Méthode managée appelée à la place</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public NativeHook AjouterNativeHook(string nomLibrairie, string nomMethode, MethodInfo methodTo, bool autoActiver = true)
        {
            return AjouterNativeHook(new NativeMethod(nomMethode, nomLibrairie), methodTo, autoActiver);
        }

        /// <summary>
        /// Faire une substitution d'une méthode Native (non managée) à une méthode managée
        /// </summary>
        /// <param name="nomLibrairie">Nom de la librairie contenant la méthode à remplacer (Exemple "user32.dll")</param>
        /// <param name="nomMethode">Nom de la méthode non managée, de la librairie, à remplacer</param>
        /// <param name="methodTo">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public NativeHook AjouterNativeHook(string nomLibrairie, string nomMethode, string methodTo, bool autoActiver = true)
        {
            MethodInfo mi = ExtensionsMethod.RetourneMethodeParLeNomComplet(methodTo);
            return AjouterNativeHook(new NativeMethod(nomMethode, nomLibrairie), mi, autoActiver);
        }

        /// <summary>
        /// Faire une substitution d'une méthode dans le GAC
        /// Attention, cette méthode de substitution est threadsafe mais pas multi-thread. Si plusieurs thread appel simultanément la méthode remplacée, elles attendront une par une, chacun leur tour, l'appel à la méthode
        /// Cette méthode de substitution est réservée au remplacement de méthode du NET Framework lui même (GlobalAssemblyCache) ou des méthodes managées pour lequel vous n'avez pas le code source et qui sont compilées avec l'optimiseur JIT activé
        /// </summary>
        /// <param name="methodFrom">Méthode managée à remplacer</param>
        /// <param name="methodTo">Méthode managée appelée à la place</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public GACHook AjouterGACHook(MethodInfo methodFrom, MethodInfo methodTo, bool autoActiver = true)
        {
            if (methodFrom == null)
                throw new ArgumentNullException(nameof(methodFrom));
            if (methodFrom.DeclaringType?.Assembly == Assembly.GetExecutingAssembly())
                throw new DoNotHookMyLib();
            if (_listeGACHook.Keys.Contains(methodFrom))
                throw new MethodAlreadyHooked(methodFrom.Name);

            GACHook gachook = new(methodFrom, methodTo);
            if (gachook != null)
            {
                if (autoActiver) gachook.Apply();
                _listeGACHook.Add(methodFrom, gachook);
            }
            return gachook;
        }

        /// <summary>
        /// Faire une substitution d'une méthode dans le GAC
        /// Attention, cette méthode de substitution est threadsafe mais pas multi-thread. Si plusieurs thread appel simultanément la méthode remplacée, elles attendront une par une, chacun leur tour, l'appel à la méthode
        /// Cette méthode de substitution est réservée au remplacement de méthode du NET Framework lui même (GlobalAssemblyCache) ou des méthodes managées pour lequel vous n'avez pas le code source et qui sont compilées avec l'optimiseur JIT activé
        /// </summary>
        /// <param name="MethodeARemplacer">Méthode managée à remplacer</param>
        /// <param name="MethodeDeRemplacement">Méthode managée appelée à la place</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public GACHook AjouterGACHook(string MethodeARemplacer, string MethodeDeRemplacement, bool autoActiver = true)
        {
            MethodInfo miSource, miDest;

            miSource = ExtensionsMethod.RetourneMethodeParLeNomComplet(MethodeARemplacer);
            miDest = ExtensionsMethod.RetourneMethodeParLeNomComplet(MethodeDeRemplacement);

            return AjouterGACHook(miSource, miDest, autoActiver);
        }

        private static void VerifierCorrespondances(MethodBase methodFrom, MethodInfo methodTo)
        {
            if (!methodTo.IsStatic)
                throw new MethodDestinationNotStatic();

            if (methodFrom is MethodInfo miFrom)
            {
                if (miFrom.ReturnType != methodTo.ReturnType)
                {
                    if (miFrom.ReturnType == typeof(void) || methodTo.ReturnType == typeof(void))
                        throw new WrongReturnType(miFrom.ReturnType, methodTo.ReturnType);
                }
            }

            if (methodFrom.GetParameters().Length > 0)
            {
                if (methodTo.GetParameters().Length == 0)
                    throw new NotEnoughArgument(methodFrom.GetParameters().Length, methodTo.GetParameters().Length);
                if (methodTo.GetParameters()[methodTo.GetParameters().Length - 1].ParameterType.BaseType == typeof(Array)) return;
            }

            if (methodFrom.IsStatic)
            {
                if (methodFrom.GetParameters().Length > 0 && methodFrom.GetParameters().Length > methodTo.GetParameters().Length)
                    throw new NotEnoughArgument(methodFrom.GetParameters().Length, methodTo.GetParameters().Length);
            }
            else
            {
                if (methodTo.GetParameters().Length == 0)
                    throw new MissingDefaultArgument(methodTo.Name);
                if ((methodTo.GetParameters()[0].ParameterType != typeof(object)) && (methodTo.GetParameters()[0].ParameterType != methodFrom.DeclaringType))
                    throw new MissingDefaultArgument(methodTo.Name);
                if (methodFrom.GetParameters().Length > methodTo.GetParameters().Length - 1)
                    throw new NotEnoughArgument(methodFrom.GetParameters().Length, methodTo.GetParameters().Length - 1);
            }
        }

        private bool MethodeDejaDecoree(MethodBase methodeFrom)
        {
            return (_listeDecoration.Values.Any(hook => hook.EstMethodesDecoration && hook.FromMethode == methodeFrom));
        }

        private bool MethodeDejaRemplacee(MethodBase methodFrom)
        {
            return (_listeHook.Values.Any(hook => hook.FromMethode == methodFrom) || _listeRemplacement.Values.Any(hook => hook.MethodeRemplacee == methodFrom));
        }

        /// <summary>
        /// Retourne une substitution par rapport à son numéro (max. 2 milliard et quelques (un entier signé 32 bits)
        /// Attention, cette méthode est obligatoire, même si il n'y a pas de référence à cette méthode indiquée, elle est utilisée en interne par ManagedHook
        /// </summary>
        /// <param name="numHook">Numéro de la substitution</param>
        public ManagedHook RetourneHook(int numHook)
        {
            return (_listeHook.Values.SingleOrDefault(hook => hook.NumHook == numHook));
        }

        /// <summary>
        /// Retourne la substitution de la méthode de remplacement en cours
        /// Ou de la décoration d'une méthode en cours
        /// </summary>
        public ManagedHook RetourneHook()
        {
            int numHook = 0;
            StackFrame[] stacks = new StackTrace(_modeDebugInterne).GetFrames();
            for (int i = stacks.Length - 1;i >= 0; i--)
            {
                if (stacks[i].GetMethod().DeclaringType.Name.StartsWith(NOM_CLASSE))
                {
                    numHook = int.Parse(stacks[i].GetMethod().DeclaringType.Name.Replace(NOM_CLASSE, ""));
                    break;
                }
            }
            return RetourneHook(numHook);
        }

        /// <summary>
        /// Retourne une substitution d'une méthode managée par une autre
        /// </summary>
        /// <param name="methodeSource">Méthode substituée</param>
        public ManagedHook RetourneHook(MethodInfo methodeSource)
        {
            return (_listeHook.Values.SingleOrDefault(hook => hook.FromMethode == methodeSource));
        }

        /// <summary>
        /// Retourne une substitution d'une méthode par une autre
        /// </summary>
        /// <param name="methodeSource">Méthode qui a été remplacée</param>
        public MethodeRemplacementHook RetourneRemplacementHook(MethodInfo methodeSource)
        {
            return (_listeRemplacement.Values.SingleOrDefault(hook => hook.MethodeRemplacee == methodeSource));
        }

        /// <summary>
        /// Retourne le premier remplacement de méthode trouvée (si il y en a plusieurs), par rapport à sa nouvelle méthode de remplacement en cours
        /// </summary>
        public MethodeRemplacementHook RetourneRemplacementHook()
        {
            StackFrame[] piles = new StackTrace(_modeDebugInterne).GetFrames();
            for (int i = piles.Length - 1; i >= 0; i--)
            {
                if (piles[i].GetMethod() is MethodInfo methodeSource)
                {
                    MethodeRemplacementHook mrh = _listeRemplacement.Values.FirstOrDefault(hook => hook.MethodeDeRemplacement == methodeSource);
                    if (mrh != null)
                        return mrh;
                }
            }
            return null;
        }

        /// <summary>
        /// Retourne toutes les méthodes que remplace une méthode en particulier
        /// </summary>
        public MethodeRemplacementHook[] RetourneRemplacementHooks(MethodInfo methodeRemplacement)
        {
            return (_listeRemplacement.Values.Where(hook => hook.MethodeDeRemplacement == methodeRemplacement).ToArray());
        }

        /// <summary>
        /// Retourne toutes les méthodes que remplace la premère méthode de remplacement en cours trouvée
        /// </summary>
        public MethodeRemplacementHook[] RetourneRemplacementHooks()
        {
            StackFrame[] piles = new StackTrace(_modeDebugInterne).GetFrames();
            for (int i = piles.Length - 1; i >= 0; i--)
            {
                if (piles[i].GetMethod() is MethodInfo methodeSource)
                {
                    MethodeRemplacementHook[] mrh = _listeRemplacement.Values.Where(hook => hook.MethodeDeRemplacement == methodeSource).ToArray();
                    if ((mrh != null) && (mrh.Length > 0))
                        return mrh;
                }
            }
            return null;
        }

        /// <summary>
        /// Retourne la substitution d'une méthode Native à une méthode managée
        /// </summary>
        /// <param name="methodFrom">Objet NativeMethod contenant le nom du module (avec son extension) ainsi que le nom de la méthode native substituée</param>
        public NativeHook RetourneNativeHook(NativeMethod methodFrom)
        {
            return (_listeNativeHook.SingleOrDefault(hook => hook.Key.ModuleName == methodFrom.ModuleName && hook.Key.Method == methodFrom.Method).Value);
        }

        /// <summary>
        /// Retourne la substitution d'une méthode Native à une méthode managée
        /// </summary>
        /// <param name="module">Nom du module (avec son extension)</param>
        /// <param name="methode">Nom de la méthode native substituée</param>
        public NativeHook RetourneNativeHook(string module, string methode)
        {
            return (_listeNativeHook.SingleOrDefault(hook => hook.Key.ModuleName == module && hook.Key.Method == methode).Value);
        }

        /// <summary>
        /// Retourne la substitution d'une méthode managée du GAC ou compilée avec l'optimiseur JIT
        /// </summary>
        /// <param name="methodFrom">Nom de la méthode substituée</param>
        /// <returns></returns>
        public GACHook RetourneGACHook(MethodInfo methodFrom)
        {
            return (_listeGACHook[methodFrom]);
        }

        /// <summary>
        /// Remplace toutes les méthodes d'une interface par les mêmes noms dans une classe static
        /// </summary>
        /// <typeparam name="TypeInterface">Type de l'interface à remplacer</typeparam>
        /// <typeparam name="TypeRemplacement">Type de classe static de remplacement</typeparam>
        public void RemplaceMethodesInterface<TypeInterface, TypeRemplacement>() where TypeInterface : class where TypeRemplacement : class
        {
            RemplaceMethodesInterface<TypeInterface, TypeRemplacement>("");
        }

        /// <summary>
        /// Remplace toutes les méthodes d'une interface par les mêmes noms dans une classe static
        /// </summary>
        /// <param name="TypeInterface">Type de l'interface à remplacer</param>
        /// <param name="TypeRemplacement">Type de classe static de remplacement</param>
        /// <param name="autoActiver">Active la substitution immédiatement (Facultatif, oui par défaut)</param>
        public void RemplaceMethodesInterface(Type TypeInterface, Type TypeRemplacement, bool autoActiver = true)
        {
            RemplaceMethodesInterface(TypeInterface, TypeRemplacement, "", autoActiver);
        }

        /// <summary>
        /// Remplace une méthode d'une interface par la même (portant le même nom) dans une classe static
        /// </summary>
        /// <typeparam name="TypeInterface">Type de l'interface à remplacer</typeparam>
        /// <typeparam name="TypeRemplacement">Type de classe static de remplacement</typeparam>
        /// <param name="nomMethode">Nom de la méthode à remplacer</param>
        /// <param name="autoActiver">Active la substitution immédiatement (Facultatif, oui par défaut)</param>
        public void RemplaceMethodesInterface<TypeInterface, TypeRemplacement>(string nomMethode, bool autoActiver = true) where TypeInterface : class where TypeRemplacement : class
        {
            RemplaceMethodesInterface(typeof(TypeInterface), typeof(TypeRemplacement), nomMethode, autoActiver);
        }

        /// <summary>
        /// Remplace une méthode d'une interface par la même (portant le même nom) dans une classe static
        /// </summary>
        /// <param name="TypeInterface">Type de l'interface à remplacer</param>
        /// <param name="TypeRemplacement">Type de classe static de remplacement</param>
        /// <param name="nomMethode">Nom de la méthode à remplacer</param>
        /// <param name="autoActiver">Active la substitution immédiatement (Facultatif, oui par défaut)</param>
        public void RemplaceMethodesInterface(Type TypeInterface, Type TypeRemplacement, string nomMethode, bool autoActiver)
        {
            if (TypeInterface == null)
                throw new ArgumentNullException(nameof(TypeInterface));
            if (TypeRemplacement == null)
                throw new ArgumentNullException(nameof(TypeRemplacement));

            if (!TypeInterface.IsInterface)
                throw new NotInterface(TypeInterface);

            MethodInfo[] listeMethodes;
            if (string.IsNullOrWhiteSpace(nomMethode))
            {
                listeMethodes = TypeInterface.GetMethods();
            }
            else
            {
                if (TypeInterface.GetMethod(nomMethode) == null)
                    throw new TypeOrMethodNotFound(TypeInterface.Name, nomMethode);

                listeMethodes = new MethodInfo[1] { TypeInterface.GetMethod(nomMethode) };
            }

            if ((listeMethodes != null) && (listeMethodes.Length > 0))
            {
                List<Type> listeTypesImplement = AppDomain.CurrentDomain.GetAssemblies().SelectMany((asm) => asm.GetTypes()).Where((type) => type.GetInterfaces().Contains(TypeInterface)).ToList();
                foreach (Type typeEnCours in listeTypesImplement)
                {
                    foreach (MethodInfo mi in listeMethodes)
                    {
                        AjouterHook(typeEnCours.GetMethod(mi.Name), TypeRemplacement.GetMethod(mi.Name), autoActiver);
                    }
                }
            }
        }

        /// <summary>
        /// Parcours tous les Assembly et prépare toutes les substitutions de toutes les méthodes qui ont l'attribut HookManager
        /// </summary>
        /// <param name="throwError">Si une erreur se produit, faut-il stoper et indiquer l'erreur (par défaut), ou ne rien faire</param>
        public void InitialiseTousHookParAttribut(bool throwError = true)
        {
            Dictionary<Type, string> dejaTraitee = new();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GlobalAssemblyCache)
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        // La classe a un attribut pour hooker toutes les méthodes
                        if (type.GetCustomAttributes<HookClasseAttribute>(false).Any())
                        {
                            foreach (HookClasseAttribute attrib in type.GetCustomAttributes<HookClasseAttribute>(false))
                            {
                                foreach (MethodInfo mi in type.GetMethods(attrib.filtreMethode))
                                {
                                    // La méthode de cette classe ne doit par contre pas avoir l'attribut spécial de remplacement propre à cette méthode (géré plus bas)
                                    if (mi.GetCustomAttribute<HookMethodeAttribute>(false) == null)
                                    {
                                        try
                                        {
                                            string nomMethode = attrib.prefixNomMethode + mi.Name + attrib.suffixeNomMethode;
                                            MethodInfo md = attrib.Classe.GetMethod(nomMethode, _filtres);
                                            if (md == null)
                                                throw new TypeOrMethodNotFound(attrib.Classe.ToString(), nomMethode);

                                            AjouterHook(mi, md, attrib.AutoActiver);
                                        }
                                        catch (Exception)
                                        {
                                            if (throwError)
                                                throw;
                                        }
                                    }
                                }
                            }
                        }

                        // On parcours toutes les méthodes qui ont un attribut de "hookage"
                        foreach (MethodInfo mi in type.GetMethods(_filtres).Where(methode => methode.GetCustomAttribute<HookMethodeAttribute>(false) != null))
                        {
                            HookMethodeAttribute monAttribut;
                            monAttribut = mi.GetCustomAttribute<HookMethodeAttribute>(false);
                            try
                            {
                                MethodInfo md = monAttribut.Classe.GetMethod(monAttribut.NomMethode, _filtres);
                                if (md == null)
                                    throw new TypeOrMethodNotFound(monAttribut.Classe.ToString(), monAttribut.NomMethode);

                                AjouterHook(mi, md, monAttribut.AutoActiver);
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // On parcours toutes les méthodes qui ont un attribut de remplacement
                        foreach (MethodInfo mi in type.GetMethods(_filtres).Where(methode => methode.GetCustomAttribute<MethodeRemplacementAttribute>(false) != null))
                        {
                            MethodeRemplacementAttribute attrib;
                            attrib = mi.GetCustomAttribute<MethodeRemplacementAttribute>(false);
                            try
                            {
                                MethodInfo md = attrib.Classe.GetMethod(attrib.NomMethode, _filtres);
                                if (md == null)
                                    throw new TypeOrMethodNotFound(attrib.Classe.ToString(), attrib.NomMethode);

                                AjouterRemplacement(mi, md, attrib.AutoActiver);
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // On parcours toutes les méthodes qui ont l'attribut de décoration
                        foreach (MethodInfo mi in type.GetMethods(_filtres).Where(methode => methode.GetCustomAttribute<DecorationMethodeAttribute>(false) != null))
                        {
                            DecorationMethodeAttribute attrib = mi.GetCustomAttribute<DecorationMethodeAttribute>(false);
                            try
                            {
                                MethodInfo miAvant = null, miApres = null;
                                if (!string.IsNullOrWhiteSpace(attrib.NomMethodeAvant))
                                    miAvant = ExtensionsMethod.RetourneMethodeParLeNomComplet(attrib.NomMethodeAvant);
                                if (!string.IsNullOrWhiteSpace(attrib.NomMethodeApres))
                                    miApres = ExtensionsMethod.RetourneMethodeParLeNomComplet(attrib.NomMethodeApres);

                                AjouterDecoration(mi, miAvant, miApres, attrib.AutoActiver);
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // La classe implémente au moins une interface qui a l'attribut de "hookage"
                        if (type.GetInterfaces().Any((interf) => interf.GetCustomAttributes<HookInterfaceAttribute>(false).Any()))
                        {
                            // On parcours toutes les interfaces (qui ont l'attribut) pour les remplacer
                            foreach (Type interf in type.GetInterfaces().Where((inter) => inter.GetCustomAttributes<HookInterfaceAttribute>(false).Any()))
                            {
                                foreach (HookInterfaceAttribute attrib in type.GetCustomAttributes<HookInterfaceAttribute>(false))
                                {
                                    // Sauf si elle a déjà été traitée sans Nom de Méthode
                                    if (!dejaTraitee.ContainsKey(interf) || (dejaTraitee[interf] != ""))
                                    {
                                        try
                                        {
                                            RemplaceMethodesInterface(interf, attrib.Classe, attrib.NomMethode, attrib.AutoActiver);
                                        }
                                        catch (Exception)
                                        {
                                            if (throwError)
                                                throw;
                                        }
                                        finally
                                        {
                                            if (dejaTraitee.ContainsKey(interf) && (dejaTraitee[interf] != "") && (attrib.NomMethode == ""))
                                                dejaTraitee[interf] = "";
                                            else if (!dejaTraitee.ContainsKey(interf))
                                                dejaTraitee.Add(interf, attrib.NomMethode);
                                        }
                                    }
                                }
                            }
                        }

                        // On parcours toutes les propriétés qui ont l'attribut de remplacement
                        foreach (PropertyInfo pi in type.GetProperties(_filtres).Where((prop) => prop.GetCustomAttribute<HookProprieteAttribute>() != null))
                        {
                            try
                            {
                                HookProprieteAttribute attrib = pi.GetCustomAttribute<HookProprieteAttribute>();
                                if (!string.IsNullOrWhiteSpace(attrib.GetMethode))
                                {
                                    AjouterHook(pi.GetGetMethod(), attrib.Classe.GetMethod(attrib.GetMethode, _filtres), attrib.AutoActiver);
                                }
                                if (!string.IsNullOrWhiteSpace(attrib.SetMethode))
                                {
                                    AjouterHook(pi.GetSetMethod(), attrib.Classe.GetMethod(attrib.SetMethode, _filtres), attrib.AutoActiver);
                                }
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // On parcours les constructeurs de cette classe qui ont l'attribut de remplacement
                        foreach (ConstructorInfo ci in type.GetConstructors(_filtres).Where((ctor) => ctor.GetCustomAttribute<HookConstructeurAttribute>() != null))
                        {
                            try
                            {
                                HookConstructeurAttribute attrib = ci.GetCustomAttribute<HookConstructeurAttribute>();

                                MethodInfo md = attrib.Classe.GetMethod(attrib.NomMethode, _filtres);
                                if (md == null)
                                    throw new TypeOrMethodNotFound(attrib.Classe.ToString(), attrib.NomMethode);

                                AjouterHook(ci, md, attrib.AutoActiver);
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }
                    }
                }
            }
        }

        private ModuleBuilder _moduleBuilder;
        private AssemblyBuilder _asmBuilder;

        internal TypeBuilder Constructeur(uint numHook)
        {
            if (_moduleBuilder == null)
            {
                AssemblyName an = new("HookManagerDynamic");
                _asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, (_modeDebugInterne ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run), System.IO.Path.GetTempPath());
                _moduleBuilder = _asmBuilder.DefineDynamicModule(an.Name, an.Name + ".dll");
            }
            TypeBuilder tb = _moduleBuilder.DefineType("HookManager.Hooks." + NOM_CLASSE + numHook.ToString(), TypeAttributes.Public);
            return tb;
        }

        /// <summary>
        /// Uniquement pour déboggage de la librairie HookManager et en mode MethodBuilder uniquement (C'est à dire sans déboggeur attaché)<br/>
        /// Si vous avez un déboggeur attaché, il faut utiliser la propriété <see cref="ModeDebugInterne"/>, les assembly se trouveront dans le répertoire TEMP de l'utilisateur courant
        /// </summary>
        public void SauveAssembly()
        {
            if (_asmBuilder != null)
                _asmBuilder.Save(_asmBuilder.GetName().Name + ".dll");
        }

        internal CSharpCodeProvider Compilateur()
        {
            if (_compilateur == null)
            {
                _compilateur = new();
            }
            return _compilateur;
        }
    }
}
