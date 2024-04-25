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
        private Dictionary<uint, ManagedHook> _listHook;
        private Dictionary<MethodInfo, GacHook> _listGacHook;
        private Dictionary<NativeMethod, NativeHook> _listNativeHook;
        private Dictionary<uint, MethodeRemplacementHook> _listReplacement;
        private Dictionary<uint, ManagedHook> _listDecoration;

        private readonly object _lockNumHook;

        private uint _nbHook;
        private static HookPool _instance;

        private CSharpCodeProvider _compiler;

        internal const string CLASS_NAME = "HookClasse_";
        internal const string METHOD_NAME = "HookMethode_";
        internal const string PARENT_METHOD_NAME = "InvokeParente_";

        internal HookPool()
        {
            _lockNumHook = new();
            ModeInternalDebug = false;
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
            _listHook = [];
            _listGacHook = [];
            _listNativeHook = [];
            _listReplacement = [];
            _listDecoration = [];

            _nbHook = 0;
            DefaultFilters = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        }

        /// <summary>
        /// Permet de rentrer en pas à pas, en debuggage, dans les méthodes gérant les Hook lorsque l'on demande à faire du pas à pas dans une méthode "hookée"<br/>
        /// Activer le mode debug pas à pas dans les fichiers générés pendant l'exécution contenant les "fausses" méthodes gérant le remplacement.<br/>
        /// Ralentit le système, à n'utiliser que pour débogger la librairie HookManager (vous pouvez toujours, même sans ca, débogger du code remplacé, ca n'a pas de rapport)<br/>
        /// Utilise le répertoire TEMP (définit par la variable d'environement) pour y stocker les fichiers générés
        /// </summary>
        public bool ModeInternalDebug { get; set; }

        /// <summary>
        /// Filtres par défaut pour la recherche des méthodes managées
        /// </summary>
        public BindingFlags DefaultFilters { get; set; }

        /// <summary>
        /// Remplace le getter et/ou le setter d'une propriété
        /// </summary>
        /// <param name="propertyFrom">Propriété à remplacer</param>
        /// <param name="methodGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddPropertyReplacement(PropertyInfo propertyFrom, MethodInfo methodGet, MethodInfo methodSet, bool autoEnable = true)
        {
            if (propertyFrom == null)
                throw new ArgumentNullException(nameof(propertyFrom));
            if ((methodGet == null) && (methodSet == null))
                throw new HookPropertyException(propertyFrom.Name, "", "", EErrorCodePropertyHook.UselessReplacement);
            if (methodGet != null)
            {
                if (propertyFrom.GetGetMethod() == null)
                    throw new HookPropertyException(propertyFrom.Name, methodGet.Name, "", EErrorCodePropertyHook.NoGet);
                AddHook(propertyFrom.GetGetMethod(), methodGet, autoEnable);
            }
            if (methodSet != null)
            {
                if (propertyFrom.GetSetMethod() == null)
                    throw new HookPropertyException(propertyFrom.Name, "", methodSet.Name, EErrorCodePropertyHook.NoSet);
                AddHook(propertyFrom.GetSetMethod(), methodSet, autoEnable);
            }
        }

        /// <summary>
        /// Remplace le getter et/ou le setter d'une propriété
        /// </summary>
        /// <param name="propertyFrom">Propriété à remplacer</param>
        /// <param name="methodeGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodeSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddPropertyReplacement(PropertyInfo propertyFrom, string methodeGet, string methodeSet, bool autoEnable = true)
        {
            if (propertyFrom == null)
                throw new ArgumentNullException(nameof(propertyFrom));
            if (string.IsNullOrWhiteSpace(methodeGet) && string.IsNullOrWhiteSpace(methodeSet))
                throw new HookPropertyException(propertyFrom.Name, "", "", EErrorCodePropertyHook.UselessReplacement);
            MethodInfo miGet = null, miSet = null;
            if (!string.IsNullOrWhiteSpace(methodeGet))
                miGet = ExtensionsMethod.GetMethodByFullName(methodeGet);
            if (!string.IsNullOrWhiteSpace(methodeSet))
                miSet = ExtensionsMethod.GetMethodByFullName(methodeSet);

            AddPropertyReplacement(propertyFrom, miGet, miSet, autoEnable);
        }

        /// <summary>
        /// Remplace le getter et/ou le setter d'une propriété
        /// </summary>
        /// <param name="propertyFrom">Propriété à remplacer</param>
        /// <param name="methodGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="methodSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddPropertyReplacement(string propertyFrom, string methodGet, string methodSet, bool autoEnable = true)
        {
            if (string.IsNullOrWhiteSpace(propertyFrom))
                throw new ArgumentNullException(nameof(propertyFrom));
            if (string.IsNullOrWhiteSpace(methodGet) && string.IsNullOrWhiteSpace(methodSet))
                throw new HookPropertyException(propertyFrom, "", "", EErrorCodePropertyHook.UselessReplacement);
            PropertyInfo piSource = null;
            if (propertyFrom.IndexOf(".") >= 0)
            {
                string nomClasse = propertyFrom.Substring(0, propertyFrom.LastIndexOf("."));
                Type classeSource = AppDomain.CurrentDomain.GetAssemblies().SearchType(nomClasse) ?? throw new NoTypeInNameException(propertyFrom);
                string prop = propertyFrom.Substring(propertyFrom.LastIndexOf(".") + 1);
                piSource = classeSource.GetProperty(prop, DefaultFilters);
            }

            if (piSource == null)
                throw new HookPropertyException(propertyFrom, "", "", EErrorCodePropertyHook.PropertyNotFound);

            MethodInfo miGet = null, miSet = null;
            if (!string.IsNullOrWhiteSpace(methodGet))
                miGet = ExtensionsMethod.GetMethodByFullName(methodGet);
            if (!string.IsNullOrWhiteSpace(methodSet))
                miSet = ExtensionsMethod.GetMethodByFullName(methodSet);

            AddPropertyReplacement(piSource, miGet, miSet, autoEnable);
        }

        /// <summary>
        /// Remplace le getter d'une propriété
        /// </summary>
        /// <param name="propertyFrom">Propriété à remplacer</param>
        /// <param name="methodGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddGetterPropertyReplacement(string propertyFrom, string methodGet, bool autoEnable = true)
        {
            AddPropertyReplacement(propertyFrom, methodGet, "", autoEnable);
        }

        /// <summary>
        /// Remplace le getter d'une propriété
        /// </summary>
        /// <param name="propertyFrom">Propriété à remplacer</param>
        /// <param name="methodGet">Nouvelle méthode pour le get de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddGetterPropertyReplacement(PropertyInfo propertyFrom, MethodInfo methodGet, bool autoEnable = true)
        {
            AddPropertyReplacement(propertyFrom, methodGet, null, autoEnable);
        }

        /// <summary>
        /// Remplace le setter d'une propriété
        /// </summary>
        /// <param name="proprieteFrom">Propriété à remplacer</param>
        /// <param name="methodSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddSetterPropertyReplacement(string proprieteFrom, string methodSet, bool autoEnable = true)
        {
            AddPropertyReplacement(proprieteFrom, "", methodSet, autoEnable);
        }

        /// <summary>
        /// Remplace le setter d'une propriété
        /// </summary>
        /// <param name="propertyFrom">Propriété à remplacer</param>
        /// <param name="methodSet">Nouvelle méthode pour le set de cette propriété</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddSetterPropertyReplacement(PropertyInfo propertyFrom, MethodInfo methodSet, bool autoEnable = true)
        {
            AddPropertyReplacement(propertyFrom, null, methodSet, autoEnable);
        }

        /// <summary>
        /// Ajouter une décoration à une méthode<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après celle mentionnée.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="methodFrom">Méthode à décorer</param>
        /// <param name="methodBefore">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodAfter">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public ManagedHook AddDecoration(string methodFrom, string methodBefore, string methodAfter, bool autoEnable = true)
        {
            if (string.IsNullOrWhiteSpace(methodBefore) && string.IsNullOrWhiteSpace(methodAfter))
                throw new DecorationMethodsException();
            MethodInfo miSource, miAvant = null, miApres = null;
            miSource = ExtensionsMethod.GetMethodByFullName(methodFrom);
            if (!string.IsNullOrWhiteSpace(methodBefore))
                miAvant = ExtensionsMethod.GetMethodByFullName(methodBefore);
            if (!string.IsNullOrWhiteSpace(methodAfter))
                miApres = ExtensionsMethod.GetMethodByFullName(methodAfter);

            return AddDecoration(miSource, miAvant, miApres, autoEnable);
        }

        /// <summary>
        /// Ajouter une décoration à tout une liste méthode<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après celle mentionnée.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="methodeFrom">Liste des méthodes à décorer</param>
        /// <param name="methodBefore">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodAfter">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddDecoration(string[] methodeFrom, string methodBefore, string methodAfter, bool autoEnable = true)
        {
            if (methodeFrom == null || methodeFrom.Length == 0)
                throw new ArgumentNullException(nameof(methodeFrom));

            if (string.IsNullOrWhiteSpace(methodBefore) && string.IsNullOrWhiteSpace(methodAfter))
                throw new DecorationMethodsException();
            MethodInfo miSource, miAvant = null, miApres = null;
            if (!string.IsNullOrWhiteSpace(methodBefore))
                miAvant = ExtensionsMethod.GetMethodByFullName(methodBefore);
            if (!string.IsNullOrWhiteSpace(methodAfter))
                miApres = ExtensionsMethod.GetMethodByFullName(methodAfter);

            foreach (string methodeSource in methodeFrom)
            {
                miSource = ExtensionsMethod.GetMethodByFullName(methodeSource);
                AddDecoration(miSource, miAvant, miApres, autoEnable);
            }
        }

        /// <summary>
        /// Ajouter une décoration à tout une liste méthode<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après celle mentionnée.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="methodFrom">Liste des méthodes à décorer</param>
        /// <param name="methodBefore">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodAfter">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public void AddDecoration(MethodInfo[] methodFrom, MethodInfo methodBefore, MethodInfo methodAfter, bool autoEnable = true)
        {
            if (methodFrom == null || methodFrom.Length == 0)
                throw new ArgumentNullException(nameof(methodFrom));
            foreach (MethodInfo mi in methodFrom)
                AddDecoration(mi, methodBefore, methodAfter, autoEnable);
        }

        /// <summary>
        /// Ajouter une décoration à une méthode<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après celle mentionnée.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="methodFrom">Méthode à décorer</param>
        /// <param name="methodBefore">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodAfter">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public ManagedHook AddDecoration(MethodInfo methodFrom, MethodInfo methodBefore, MethodInfo methodAfter, bool autoEnable = true)
        {
            if (methodFrom == null)
                throw new ArgumentNullException(nameof(methodFrom));
            if (methodFrom.DeclaringType?.Assembly == Assembly.GetExecutingAssembly())
                throw new DoNotHookMyLibException();
            if (methodBefore == null && methodAfter == null)
                throw new DecorationMethodsException();
            if (MethodAlreadyDecorate(methodFrom))
                throw new MethodeAlreadyDecoratedException(methodFrom.Name);
            if (methodBefore != null)
                CheckMatches(methodFrom, methodBefore);
            if (methodAfter != null)
            {
                int nbParam = 0;
                if (methodFrom.IsStatic)
                    nbParam++;
                if (methodFrom.ReturnType != typeof(void))
                    nbParam++;
                if (methodAfter.GetParameters().Length < nbParam)
                    throw new MissingDefaultArgumentException(methodAfter.Name);
            }

            uint numHook = IncrNumHook();
            ManagedHook mh = new(numHook, methodFrom, methodBefore, methodAfter, autoEnable);
            _listHook.Add(numHook, mh);
            return mh;
        }

        /// <summary>
        /// Faire une substitution d'une méthode managée à une autre méthode managée
        /// </summary>
        /// <param name="methodFrom">Méthode managée à remplacer</param>
        /// <param name="methodTo">Méthode managée appelée à la place</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public ManagedHook AddHook(MethodInfo methodFrom, MethodInfo methodTo, bool autoEnable = true)
        {
            CommonsCheck(methodFrom, methodTo);
            CheckMatches(methodFrom, methodTo);
            uint numHook = IncrNumHook();
            ManagedHook hook = new(numHook, methodFrom, methodTo, autoEnable);
            _listHook.Add(numHook, hook);
            return hook;
        }

        /// <summary>
        /// Intercepte lorsqu'un nouvel abonné arrive sur un Event, ou qu'on supprime un abonné
        /// </summary>
        /// <param name="event">Evenement à surveiller</param>
        /// <param name="addSubscriber">Méthode exécutée lors de l'ajout d'un nouvel abonné à l'évènement</param>
        /// <param name="removeSubscriber">Méthode exécutée lors de la suppression d'un abonné à l'évènement</param>
        /// <param name="autoEnable">Active ou non tout de suite l'interception</param>
        public ManagedHook[] AddHook(EventInfo @event, MethodInfo addSubscriber = null, MethodInfo removeSubscriber = null, bool autoEnable = true)
        {
            if (addSubscriber == null && removeSubscriber == null)
                throw new NoMethodForEventException();

            ManagedHook hookAjout, hookSupprime;
            List<ManagedHook> listeRetour = [];
            if (addSubscriber != null)
            {
                CommonsCheck(@event.AddMethod, addSubscriber);
                CheckMatches(@event.AddMethod, addSubscriber);
                uint numHook = IncrNumHook();
                hookAjout = new(numHook, @event.AddMethod, addSubscriber, autoEnable);
                _listHook.Add(numHook, hookAjout);
                listeRetour.Add(hookAjout);
            }
            if (removeSubscriber != null)
            {
                CommonsCheck(@event.RemoveMethod, removeSubscriber);
                CheckMatches(@event.RemoveMethod, removeSubscriber);
                uint numHook = IncrNumHook();
                hookSupprime = new(numHook, @event.RemoveMethod, removeSubscriber, autoEnable);
                _listHook.Add(numHook, hookSupprime);
                listeRetour.Add(hookSupprime);
            }
            return listeRetour.ToArray();
        }

        /// <summary>
        /// Faire une substitution d'un constructeur vers une autre méthode managée
        /// </summary>
        /// <param name="constructorToReplace">Constructeur à remplacer</param>
        /// <param name="methodeTo">Méthode remplacant le constructeur</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public ManagedHook AddHook(ConstructorInfo constructorToReplace, MethodInfo methodeTo, bool autoEnable = true)
        {
            if (constructorToReplace == null)
                throw new ArgumentNullException(nameof(constructorToReplace));
            CommonsCheck(constructorToReplace, methodeTo);
            CheckMatches(constructorToReplace, methodeTo);
            uint numHook = IncrNumHook();
            ManagedHook hook = new(numHook, constructorToReplace, methodeTo, autoEnable);
            _listHook.Add(numHook, hook);
            return hook;
        }

        /// <summary>
        /// Faire une substitution d'une méthode managée à une autre méthode managée
        /// </summary>
        /// <param name="MethodToReplace">Nom de la méthode (avec le type, et l'espace de nom au besoin) à remplacer. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="MethodReplacement">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public ManagedHook AddHook(string MethodToReplace, string MethodReplacement, bool autoEnable = true)
        {
            MethodInfo miSource, miDest;

            miSource = ExtensionsMethod.GetMethodByFullName(MethodToReplace);
            miDest = ExtensionsMethod.GetMethodByFullName(MethodReplacement);

            return AddHook(miSource, miDest, autoEnable);
        }

        /// <summary>
        /// Ajouter une décoration à un constructeur<br/>
        /// C'est à dire, la possibilité d'exécuter une méthode avant et/ou une méthode après le constructeur mentionné.<br/>
        /// Bien sur, les méthodes avant et après sont facultatives, mais il en faut au moins 1 des 2, sinon la décoration ne sert à rien
        /// </summary>
        /// <param name="constructorToDecorate">Constructeur à décorer</param>
        /// <param name="methodBefore">Méthode à exécuter avant (si besoin)</param>
        /// <param name="methodAfter">Méthode à exécuter après (si besoin)</param>
        /// <param name="autoEnable">Active ou non tout de suite la décoration</param>
        public ManagedHook AddConstructorDecoration(ConstructorInfo constructorToDecorate, MethodInfo methodBefore, MethodInfo methodAfter, bool autoEnable = true)
        {
            if (constructorToDecorate == null)
                throw new ArgumentNullException(nameof(constructorToDecorate));
            if (constructorToDecorate.DeclaringType?.Assembly == Assembly.GetExecutingAssembly())
                throw new DoNotHookMyLibException();
            if (methodBefore == null && methodAfter == null)
                throw new DecorationMethodsException();
            if (MethodAlreadyDecorate(constructorToDecorate))
                throw new MethodeAlreadyDecoratedException(constructorToDecorate.Name);
            if (methodBefore != null)
                CheckMatches(constructorToDecorate, methodBefore);
            if (methodAfter != null)
            {
                int nbParam = 1;
                if (constructorToDecorate.IsStatic)
                    nbParam++;
                if (methodAfter.GetParameters().Length < nbParam)
                    throw new MissingDefaultArgumentException(methodAfter.Name);
            }

            uint numHook = IncrNumHook();
            ManagedHook mh = new(numHook, constructorToDecorate, methodBefore, methodAfter, autoEnable);
            _listHook.Add(numHook, mh);
            return mh;
        }

        private void CommonsCheck(MethodBase methodFrom, MethodInfo methodTo, bool testMethodFrom = true, bool testMethodTo = true)
        {
            if ((methodFrom == null) && (testMethodFrom))
                throw new ArgumentNullException(nameof(methodFrom));
            if ((methodTo == null) && (testMethodTo))
                throw new ArgumentNullException(nameof(methodTo));
            if (methodFrom?.DeclaringType == null)
                throw new CantHookDynamicMethodException(methodFrom?.Name);
            if (methodFrom.DeclaringType.Assembly.IsJITOptimizerEnabled() && Debugger.IsAttached)
                throw new CantHookJitOptimizedException(methodFrom.DeclaringType.Assembly?.GetName()?.Name);
            if (methodFrom.DeclaringType.Assembly.IsJITOptimizerEnabled() && methodFrom is MethodInfo miFrom && miFrom.ReturnType != typeof(void))
                throw new CantHookJitOptimizedException(methodFrom.DeclaringType.Assembly?.GetName()?.Name, methodFrom.Name);
            if (methodFrom.DeclaringType.Assembly.IsJITOptimizerEnabled() && methodFrom.IsConstructor)
                throw new CantHookJitOptimizedException(methodFrom.DeclaringType.Assembly?.GetName()?.Name, methodFrom.Name);
            if (methodFrom.DeclaringType.Assembly == Assembly.GetExecutingAssembly())
                throw new DoNotHookMyLibException();
            if (MethodAlreadyReplace(methodFrom))
                throw new MethodAlreadyHookedException(methodFrom.Name);
            if (methodFrom.DeclaringType.Assembly.GlobalAssemblyCache)
                throw new CantHookGacException();
            ProcessorArchitecture paFrom, paTo;
            if (testMethodFrom && testMethodTo)
            {
                paFrom = methodFrom.DeclaringType.Assembly.GetName().ProcessorArchitecture;
                paTo = methodTo.DeclaringType.Assembly.GetName().ProcessorArchitecture;
                if ((paFrom == ProcessorArchitecture.X86 && (paTo == ProcessorArchitecture.Amd64 || paTo == ProcessorArchitecture.IA64)) ||
                    ((paFrom == ProcessorArchitecture.Amd64 || paFrom == ProcessorArchitecture.IA64) && paTo == ProcessorArchitecture.X86))
                    throw new PlatformAssemblyDifferentException(paFrom, paTo);
                if (paFrom == ProcessorArchitecture.Arm || paTo == ProcessorArchitecture.Arm)
                    throw new Exceptions.PlatformNotSupportedException(ProcessorArchitecture.Arm);
            }
        }

        /// <summary>
        /// Remplace une méthode par une autre (l'ancienne devient inaccessible)
        /// </summary>
        /// <param name="methodFrom">Méthode managée à remplacer</param>
        /// <param name="methodTo">Méthode managée appelée à la place</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public MethodeRemplacementHook AddReplacement(MethodInfo methodFrom, MethodInfo methodTo, bool autoEnable = true)
        {
            CommonsCheck(methodFrom, methodTo);
            CheckMatches(methodFrom, methodTo);
            uint numHook = IncrNumHook();
            MethodeRemplacementHook hook = new(numHook, methodFrom, methodTo, autoEnable);
            _listReplacement.Add(numHook, hook);
            return hook;
        }

        /// <summary>
        /// Remplace une méthode par une autre (l'ancienne devient inaccessible)
        /// </summary>
        /// <param name="methodFrom">Nom de la méthode (avec le type, et l'espace de nom au besoin) à remplacer. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="methodTo">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        public MethodeRemplacementHook AddReplacement(string methodFrom, string methodTo, bool autoEnable = true)
        {
            MethodInfo miFrom, miTo;

            miFrom = ExtensionsMethod.GetMethodByFullName(methodFrom);
            miTo = ExtensionsMethod.GetMethodByFullName(methodTo);

            return AddReplacement(miFrom, miTo, autoEnable);
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
        /// <param name="autoEnable">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public NativeHook AddNativeHook(NativeMethod methodFrom, MethodInfo methodTo, bool autoEnable = true)
        {
            if (methodFrom == null)
                throw new ArgumentNullException(nameof(methodFrom));
            if (methodFrom.ModuleName == Assembly.GetExecutingAssembly().GetName()?.Name + ".dll")
                throw new DoNotHookMyLibException();
            if (_listNativeHook.Keys.Any(hook => hook.ModuleName == methodFrom.ModuleName && hook.Method == methodFrom.Method))
                throw new MethodAlreadyHookedException(methodFrom.Method);

            NativeHook natHook = new(methodFrom, methodTo);
            if (autoEnable)
                natHook.Enable();
            _listNativeHook.Add(methodFrom, natHook);
            return natHook;
        }

        /// <summary>
        /// Faire une substitution d'une méthode Native (non managée) à une méthode managée
        /// </summary>
        /// <param name="libraryName">Nom de la librairie contenant la méthode à remplacer (Exemple "user32.dll")</param>
        /// <param name="methodName">Nom de la méthode non managée, de la librairie, à remplacer</param>
        /// <param name="methodTo">Méthode managée appelée à la place</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public NativeHook AddNativeHook(string libraryName, string methodName, MethodInfo methodTo, bool autoActiver = true)
        {
            return AddNativeHook(new NativeMethod(methodName, libraryName), methodTo, autoActiver);
        }

        /// <summary>
        /// Faire une substitution d'une méthode Native (non managée) à une méthode managée
        /// </summary>
        /// <param name="libraryName">Nom de la librairie contenant la méthode à remplacer (Exemple "user32.dll")</param>
        /// <param name="methodName">Nom de la méthode non managée, de la librairie, à remplacer</param>
        /// <param name="methodTo">Nom de la méthode (avec le type, et l'espace de nom au besoin) de remplacement. Exemple : MonNameSpace.MaClasse.MaMethode</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public NativeHook AddNativeHook(string libraryName, string methodName, string methodTo, bool autoActiver = true)
        {
            MethodInfo mi = ExtensionsMethod.GetMethodByFullName(methodTo);
            return AddNativeHook(new NativeMethod(methodName, libraryName), mi, autoActiver);
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
        public GacHook AddGacHook(MethodInfo methodFrom, MethodInfo methodTo, bool autoActiver = true)
        {
            if (methodFrom == null)
                throw new ArgumentNullException(nameof(methodFrom));
            if (methodFrom.DeclaringType?.Assembly == Assembly.GetExecutingAssembly())
                throw new DoNotHookMyLibException();
            if (_listGacHook.Keys.Contains(methodFrom))
                throw new MethodAlreadyHookedException(methodFrom.Name);

            GacHook gachook = new(methodFrom, methodTo);
            if (autoActiver) gachook.Enable();
            _listGacHook.Add(methodFrom, gachook);
            return gachook;
        }

        /// <summary>
        /// Faire une substitution d'une méthode dans le GAC
        /// Attention, cette méthode de substitution est threadsafe mais pas multi-thread. Si plusieurs thread appel simultanément la méthode remplacée, elles attendront une par une, chacun leur tour, l'appel à la méthode
        /// Cette méthode de substitution est réservée au remplacement de méthode du NET Framework lui même (GlobalAssemblyCache) ou des méthodes managées pour lequel vous n'avez pas le code source et qui sont compilées avec l'optimiseur JIT activé
        /// </summary>
        /// <param name="MethodToReplace">Méthode managée à remplacer</param>
        /// <param name="MethodReplacement">Méthode managée appelée à la place</param>
        /// <param name="autoActiver">Active ou non tout de suite le remplacement</param>
        /// <remarks>Appeler la méthode "parente" ne supporte PAS le multithread</remarks>
        public GacHook AddGacHook(string MethodToReplace, string MethodReplacement, bool autoActiver = true)
        {
            MethodInfo miSource, miDest;

            miSource = ExtensionsMethod.GetMethodByFullName(MethodToReplace);
            miDest = ExtensionsMethod.GetMethodByFullName(MethodReplacement);

            return AddGacHook(miSource, miDest, autoActiver);
        }

        private static void CheckMatches(MethodBase methodFrom, MethodInfo methodTo)
        {
            if (!methodTo.IsStatic)
                throw new MethodDestinationNotStaticException();

            if (methodFrom is MethodInfo miFrom &&
                miFrom.ReturnType != methodTo.ReturnType &&
                (miFrom.ReturnType == typeof(void) || methodTo.ReturnType == typeof(void)))
            {
                throw new WrongReturnTypeException(miFrom.ReturnType, methodTo.ReturnType);
            }

            if (methodFrom.GetParameters().Length > 0)
            {
                if (methodTo.GetParameters().Length == 0)
                    throw new NotEnoughArgumentException(methodFrom.GetParameters().Length, methodTo.GetParameters().Length);
                if (methodTo.GetParameters()[methodTo.GetParameters().Length - 1].ParameterType.BaseType == typeof(Array))
                    return;
            }

            if (methodFrom.IsStatic)
            {
                if (methodFrom.GetParameters().Length > 0 && methodFrom.GetParameters().Length > methodTo.GetParameters().Length)
                    throw new NotEnoughArgumentException(methodFrom.GetParameters().Length, methodTo.GetParameters().Length);
            }
            else
            {
                if (methodTo.GetParameters().Length == 0)
                    throw new MissingDefaultArgumentException(methodTo.Name);
                if ((methodTo.GetParameters()[0].ParameterType != typeof(object)) && (methodTo.GetParameters()[0].ParameterType != methodFrom.DeclaringType))
                    throw new MissingDefaultArgumentException(methodTo.Name);
                if (methodFrom.GetParameters().Length > methodTo.GetParameters().Length - 1)
                    throw new NotEnoughArgumentException(methodFrom.GetParameters().Length, methodTo.GetParameters().Length - 1);
            }
        }

        private bool MethodAlreadyDecorate(MethodBase methodFrom)
        {
            return (_listDecoration.Values.Any(hook => hook.IsDecorativeMethods && hook.FromMethod == methodFrom));
        }

        private bool MethodAlreadyReplace(MethodBase methodFrom)
        {
            return (_listHook.Values.Any(hook => hook.FromMethod == methodFrom) || _listReplacement.Values.Any(hook => hook.MethodeRemplacee == methodFrom));
        }

        /// <summary>
        /// Retourne une substitution par rapport à son numéro (max. 2 milliard et quelques (un entier signé 32 bits)
        /// Attention, cette méthode est obligatoire, même si il n'y a pas de référence à cette méthode indiquée, elle est utilisée en interne par ManagedHook
        /// </summary>
        /// <param name="numHook">Numéro de la substitution</param>
        public ManagedHook ReturnHook(int numHook)
        {
            return (_listHook.Values.SingleOrDefault(hook => hook.InternalNumHook == numHook));
        }

        /// <summary>
        /// Retourne la substitution de la méthode de remplacement en cours
        /// Ou de la décoration d'une méthode en cours
        /// </summary>
        public ManagedHook ReturnHook()
        {
            int numHook = 0;
            StackFrame[] stacks = new StackTrace(ModeInternalDebug).GetFrames();
            for (int i = stacks.Length - 1; i >= 0; i--)
            {
                if (stacks[i].GetMethod().DeclaringType.Name.StartsWith(CLASS_NAME))
                {
                    numHook = int.Parse(stacks[i].GetMethod().DeclaringType.Name.Replace(CLASS_NAME, ""));
                    break;
                }
            }
            return ReturnHook(numHook);
        }

        /// <summary>
        /// Retourne une substitution d'une méthode managée par une autre
        /// </summary>
        /// <param name="methodSource">Méthode substituée</param>
        public ManagedHook ReturnHook(MethodInfo methodSource)
        {
            return (_listHook.Values.SingleOrDefault(hook => hook.FromMethod == methodSource));
        }

        /// <summary>
        /// Retourne une substitution d'une méthode par une autre
        /// </summary>
        /// <param name="methodSource">Méthode qui a été remplacée</param>
        public MethodeRemplacementHook ReturnReplacementHook(MethodInfo methodSource)
        {
            return (_listReplacement.Values.SingleOrDefault(hook => hook.MethodeRemplacee == methodSource));
        }

        /// <summary>
        /// Retourne le premier remplacement de méthode trouvée (si il y en a plusieurs), par rapport à sa nouvelle méthode de remplacement en cours
        /// </summary>
        public MethodeRemplacementHook ReturnReplacementHook()
        {
            StackFrame[] piles = new StackTrace(ModeInternalDebug).GetFrames();
            for (int i = piles.Length - 1; i >= 0; i--)
            {
                if (piles[i].GetMethod() is MethodInfo methodeSource)
                {
                    MethodeRemplacementHook mrh = _listReplacement.Values.FirstOrDefault(hook => hook.MethodeDeRemplacement == methodeSource);
                    if (mrh != null)
                        return mrh;
                }
            }
            return null;
        }

        /// <summary>
        /// Retourne toutes les méthodes que remplace une méthode en particulier
        /// </summary>
        public MethodeRemplacementHook[] ReturnReplacementHooks(MethodInfo methodeRemplacement)
        {
            return (_listReplacement.Values.Where(hook => hook.MethodeDeRemplacement == methodeRemplacement).ToArray());
        }

        /// <summary>
        /// Retourne toutes les méthodes que remplace la premère méthode de remplacement en cours trouvée
        /// </summary>
        public MethodeRemplacementHook[] ReturnReplacementHooks()
        {
            StackFrame[] piles = new StackTrace(ModeInternalDebug).GetFrames();
            for (int i = piles.Length - 1; i >= 0; i--)
            {
                if (piles[i].GetMethod() is MethodInfo methodeSource)
                {
                    MethodeRemplacementHook[] mrh = _listReplacement.Values.Where(hook => hook.MethodeDeRemplacement == methodeSource).ToArray();
                    if (mrh.Length > 0)
                        return mrh;
                }
            }
            return [];
        }

        /// <summary>
        /// Retourne la substitution d'une méthode Native à une méthode managée
        /// </summary>
        /// <param name="methodFrom">Objet NativeMethod contenant le nom du module (avec son extension) ainsi que le nom de la méthode native substituée</param>
        public NativeHook ReturnNativeHook(NativeMethod methodFrom)
        {
            return (_listNativeHook.SingleOrDefault(hook => hook.Key.ModuleName == methodFrom.ModuleName && hook.Key.Method == methodFrom.Method).Value);
        }

        /// <summary>
        /// Retourne la substitution d'une méthode Native à une méthode managée
        /// </summary>
        /// <param name="libraryName">Nom du module (avec son extension)</param>
        /// <param name="methodName">Nom de la méthode native substituée</param>
        public NativeHook ReturnNativeHook(string libraryName, string methodName)
        {
            return (_listNativeHook.SingleOrDefault(hook => hook.Key.ModuleName == libraryName && hook.Key.Method == methodName).Value);
        }

        /// <summary>
        /// Retourne la substitution d'une méthode managée du GAC ou compilée avec l'optimiseur JIT
        /// </summary>
        /// <param name="methodFrom">Nom de la méthode substituée</param>
        /// <returns></returns>
        public GacHook ReturnGacHook(MethodInfo methodFrom)
        {
            return (_listGacHook[methodFrom]);
        }

        /// <summary>
        /// Remplace toutes les méthodes d'une interface par les mêmes noms dans une classe static
        /// </summary>
        /// <typeparam name="TypeInterface">Type de l'interface à remplacer</typeparam>
        /// <typeparam name="TypeRemplacement">Type de classe static de remplacement</typeparam>
        public void ReplaceInterfaceMethods<TypeInterface, TypeRemplacement>() where TypeInterface : class where TypeRemplacement : class
        {
            ReplaceInterfaceMethod<TypeInterface, TypeRemplacement>("");
        }

        /// <summary>
        /// Remplace toutes les méthodes d'une interface par les mêmes noms dans une classe static
        /// </summary>
        /// <param name="TypeInterface">Type de l'interface à remplacer</param>
        /// <param name="TypeReplacement">Type de classe static de remplacement</param>
        /// <param name="autoEnable">Active la substitution immédiatement (Facultatif, oui par défaut)</param>
        public void ReplaceInterfaceMethods(Type TypeInterface, Type TypeReplacement, bool autoEnable = true)
        {
            ReplaceInterfaceMethod(TypeInterface, TypeReplacement, "", autoEnable);
        }

        /// <summary>
        /// Remplace une méthode d'une interface par la même (portant le même nom) dans une classe static
        /// </summary>
        /// <typeparam name="TypeInterface">Type de l'interface à remplacer</typeparam>
        /// <typeparam name="TypeRemplacement">Type de classe static de remplacement</typeparam>
        /// <param name="methodName">Nom de la méthode à remplacer</param>
        /// <param name="autoEnable">Active la substitution immédiatement (Facultatif, oui par défaut)</param>
        public void ReplaceInterfaceMethod<TypeInterface, TypeRemplacement>(string methodName, bool autoEnable = true) where TypeInterface : class where TypeRemplacement : class
        {
            ReplaceInterfaceMethod(typeof(TypeInterface), typeof(TypeRemplacement), methodName, autoEnable);
        }

        /// <summary>
        /// Remplace une méthode d'une interface par la même (portant le même nom) dans une classe static
        /// </summary>
        /// <param name="TypeInterface">Type de l'interface à remplacer</param>
        /// <param name="TypeReplacement">Type de classe static de remplacement</param>
        /// <param name="methodName">Nom de la méthode à remplacer</param>
        /// <param name="autoEnable">Active la substitution immédiatement (Facultatif, oui par défaut)</param>
        public void ReplaceInterfaceMethod(Type TypeInterface, Type TypeReplacement, string methodName, bool autoEnable)
        {
            if (TypeInterface == null)
                throw new ArgumentNullException(nameof(TypeInterface));
            if (TypeReplacement == null)
                throw new ArgumentNullException(nameof(TypeReplacement));

            if (!TypeInterface.IsInterface)
                throw new NotInterfaceException(TypeInterface);

            MethodInfo[] listeMethodes;
            if (string.IsNullOrWhiteSpace(methodName))
            {
                listeMethodes = TypeInterface.GetMethods();
            }
            else
            {
                if (TypeInterface.GetMethod(methodName) == null)
                    throw new TypeOrMethodNotFoundException(TypeInterface.Name, methodName);

                listeMethodes = [TypeInterface.GetMethod(methodName)];
            }

            if ((listeMethodes != null) && (listeMethodes.Length > 0))
            {
                List<Type> listeTypesImplement = AppDomain.CurrentDomain.GetAssemblies().SelectMany((asm) => asm.GetTypes()).Where((type) => type.GetInterfaces().Contains(TypeInterface)).ToList();
                foreach (Type typeEnCours in listeTypesImplement)
                {
                    foreach (string miName in listeMethodes.Select(m => m.Name))
                    {
                        AddHook(typeEnCours.GetMethod(miName), TypeReplacement.GetMethod(miName), autoEnable);
                    }
                }
            }
        }

        /// <summary>
        /// Parcours tous les Assembly et prépare toutes les substitutions de toutes les méthodes qui ont l'attribut HookManager
        /// </summary>
        /// <param name="throwError">Si une erreur se produit, faut-il stoper et indiquer l'erreur (par défaut), ou ne rien faire</param>
        public void InitializeAllAttributeHook(bool throwError = true)
        {
            Dictionary<Type, string> alreadyDone = [];
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
                                foreach (MethodInfo mi in type.GetMethods(attrib.FiltreMethode))
                                {
                                    // La méthode de cette classe ne doit par contre pas avoir l'attribut spécial de remplacement propre à cette méthode (géré plus bas)
                                    if (mi.GetCustomAttribute<HookMethodAttribute>(false) == null)
                                    {
                                        try
                                        {
                                            string nomMethode = attrib.PrefixNomMethode + mi.Name + attrib.SuffixeNomMethode;
                                            MethodInfo md = attrib.Classe.GetMethod(nomMethode, DefaultFilters) ?? throw new TypeOrMethodNotFoundException(attrib.Classe.ToString(), nomMethode);
                                            AddHook(mi, md, attrib.AutoEnabled);
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
                        foreach (MethodInfo mi in type.GetMethods(DefaultFilters).Where(method => method.GetCustomAttribute<HookMethodAttribute>(false) != null))
                        {
                            HookMethodAttribute monAttribut;
                            monAttribut = mi.GetCustomAttribute<HookMethodAttribute>(false);
                            try
                            {
                                MethodInfo md = monAttribut.Classe.GetMethod(monAttribut.MethodName, DefaultFilters) ?? throw new TypeOrMethodNotFoundException(monAttribut.Classe.ToString(), monAttribut.MethodName);
                                AddHook(mi, md, monAttribut.AutoEnabled);
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // On parcours toutes les méthodes qui ont un attribut de remplacement
                        foreach (MethodInfo mi in type.GetMethods(DefaultFilters).Where(method => method.GetCustomAttribute<ReplaceMethodAttribute>(false) != null))
                        {
                            ReplaceMethodAttribute attrib;
                            attrib = mi.GetCustomAttribute<ReplaceMethodAttribute>(false);
                            try
                            {
                                MethodInfo md = attrib.Classe.GetMethod(attrib.MethodName, DefaultFilters) ?? throw new TypeOrMethodNotFoundException(attrib.Classe.ToString(), attrib.MethodName);
                                AddReplacement(mi, md, attrib.AutoEnabled);
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // On parcours toutes les méthodes qui ont l'attribut de décoration
                        foreach (MethodInfo mi in type.GetMethods(DefaultFilters).Where(method => method.GetCustomAttribute<DecorateMethodAttribute>(false) != null))
                        {
                            DecorateMethodAttribute attrib = mi.GetCustomAttribute<DecorateMethodAttribute>(false);
                            try
                            {
                                MethodInfo miAvant = null, miApres = null;
                                if (!string.IsNullOrWhiteSpace(attrib.MethodNameBefore))
                                    miAvant = ExtensionsMethod.GetMethodByFullName(attrib.MethodNameBefore);
                                if (!string.IsNullOrWhiteSpace(attrib.MethodNameAfter))
                                    miApres = ExtensionsMethod.GetMethodByFullName(attrib.MethodNameAfter);

                                AddDecoration(mi, miAvant, miApres, attrib.AutoEnabled);
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // La classe implémente au moins une interface qui a l'attribut de "hookage"
                        if (Array.Exists(type.GetInterfaces(), (interf) => interf.GetCustomAttributes<HookInterfaceAttribute>(false).Any()))
                        {
                            // On parcours toutes les interfaces (qui ont l'attribut) pour les remplacer
                            foreach (Type interf in type.GetInterfaces().Where((inter) => inter.GetCustomAttributes<HookInterfaceAttribute>(false).Any()))
                            {
                                foreach (HookInterfaceAttribute attrib in type.GetCustomAttributes<HookInterfaceAttribute>(false))
                                {
                                    // Sauf si elle a déjà été traitée sans Nom de Méthode
                                    if (!alreadyDone.ContainsKey(interf) || (alreadyDone[interf] != ""))
                                    {
                                        try
                                        {
                                            ReplaceInterfaceMethod(interf, attrib.Classe, attrib.NomMethode, attrib.AutoEnabled);
                                        }
                                        catch (Exception)
                                        {
                                            if (throwError)
                                                throw;
                                        }
                                        finally
                                        {
                                            if (alreadyDone.ContainsKey(interf) && (alreadyDone[interf] != "") && (attrib.NomMethode == ""))
                                                alreadyDone[interf] = "";
                                            else if (!alreadyDone.ContainsKey(interf))
                                                alreadyDone.Add(interf, attrib.NomMethode);
                                        }
                                    }
                                }
                            }
                        }

                        // On parcours toutes les propriétés qui ont l'attribut de remplacement
                        foreach (PropertyInfo pi in type.GetProperties(DefaultFilters).Where((prop) => prop.GetCustomAttribute<HookProprieteAttribute>() != null))
                        {
                            try
                            {
                                HookProprieteAttribute attrib = pi.GetCustomAttribute<HookProprieteAttribute>();
                                if (!string.IsNullOrWhiteSpace(attrib.GetMethode))
                                {
                                    AddHook(pi.GetGetMethod(), attrib.Classe.GetMethod(attrib.GetMethode, DefaultFilters), attrib.AutoEnabled);
                                }
                                if (!string.IsNullOrWhiteSpace(attrib.SetMethode))
                                {
                                    AddHook(pi.GetSetMethod(), attrib.Classe.GetMethod(attrib.SetMethode, DefaultFilters), attrib.AutoEnabled);
                                }
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // On parcours les constructeurs de cette classe qui ont l'attribut de remplacement
                        foreach (ConstructorInfo ci in type.GetConstructors(DefaultFilters).Where((ctor) => ctor.GetCustomAttribute<HookConstructeurAttribute>() != null))
                        {
                            try
                            {
                                HookConstructeurAttribute attrib = ci.GetCustomAttribute<HookConstructeurAttribute>();

                                MethodInfo md = attrib.Classe.GetMethod(attrib.NomMethode, DefaultFilters) ?? throw new TypeOrMethodNotFoundException(attrib.Classe.ToString(), attrib.NomMethode);
                                AddHook(ci, md, attrib.AutoEnabled);
                            }
                            catch (Exception)
                            {
                                if (throwError)
                                    throw;
                            }
                        }

                        // On parcours les évènements de cette classe qui ont l'attribut d'interception
                        foreach (EventInfo ei in type.GetEvents(DefaultFilters).Where(evenement => evenement.GetCustomAttribute<HookEvenementAttribute>() != null))
                        {
                            try
                            {
                                HookEvenementAttribute attrib = ei.GetCustomAttribute<HookEvenementAttribute>();

                                MethodInfo ma = null, ms = null;
                                if (!string.IsNullOrWhiteSpace(attrib.NomMethodeAjout))
                                {
                                    ma = attrib.Classe.GetMethod(attrib.NomMethodeAjout, DefaultFilters);
                                    if (ma == null)
                                        throw new TypeOrMethodNotFoundException(attrib.Classe.ToString(), attrib.NomMethodeAjout);
                                }
                                if (!string.IsNullOrWhiteSpace(attrib.NomMethodeSupprime))
                                {
                                    ma = attrib.Classe.GetMethod(attrib.NomMethodeSupprime, DefaultFilters);
                                    if (ma == null)
                                        throw new TypeOrMethodNotFoundException(attrib.Classe.ToString(), attrib.NomMethodeSupprime);
                                }
                                AddHook(ei, ma, ms, attrib.AutoEnabled);
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

        internal TypeBuilder Constructor(uint numHook)
        {
            if (_moduleBuilder == null)
            {
                AssemblyName an = new("HookManagerDynamic");
                _asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, (ModeInternalDebug ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run), System.IO.Path.GetTempPath());
                _moduleBuilder = _asmBuilder.DefineDynamicModule(an.Name, an.Name + ".dll");
            }
            TypeBuilder tb = _moduleBuilder.DefineType("HookManager.Hooks." + CLASS_NAME + numHook.ToString(), TypeAttributes.Public);
            return tb;
        }

        /// <summary>
        /// Uniquement pour déboggage de la librairie HookManager et en mode MethodBuilder uniquement (C'est à dire sans déboggeur attaché)<br/>
        /// Si vous avez un déboggeur attaché, il faut utiliser la propriété <see cref="ModeInternalDebug"/>, les assembly se trouveront dans le répertoire TEMP de l'utilisateur courant
        /// </summary>
        public void SaveAssembly()
        {
            _asmBuilder?.Save(_asmBuilder.GetName().Name + ".dll");
        }

        internal CSharpCodeProvider Compiler()
        {
            _compiler ??= new();
            return _compiler;
        }
    }
}
