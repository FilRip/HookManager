using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HookManager.Helpers;

namespace HookManager.Modeles
{
    /// <summary>
    /// Classe gérant le remplacement d'une méthode managée<br/>
    /// De vos projets/vos librairies, qui ne sont pas dans le GAC
    /// </summary>
    public sealed class ManagedHook
    {
        private readonly uint _numHook;
        private readonly MethodInfo _methodeFrom;
        private readonly MethodInfo _methodeTo;
        private MethodInfo _methodePasserelle;
        private object _maClasseDynamique;
        private MethodInfo _methodeParente;
        private bool _actif;
        private readonly ConstructorInfo _constructeur;

        private readonly bool _estMethodeDecoration;
        private readonly MethodInfo _methodeAvant;
        private readonly MethodInfo _methodeApres;

        private byte _opOriginal;
        private int _ptrOriginal;
        private uint _nouveauPtr;
        private IntPtr _ptrConstructeur;
        private DynamicMethod _copyParente;

        private readonly object _threadSafe = new();

        /// <summary>
        /// Le remplacement de la méthode est-il actuellement actif
        /// </summary>
        public bool Actif
        {
            get { return _actif; }
        }

        /// <summary>
        /// Méthode d'origine/remplacée
        /// </summary>
        public MethodInfo FromMethode
        {
            get { return _methodeFrom; }
        }

        /// <summary>
        /// Méthode de remplacement
        /// </summary>
        public MethodInfo ToMethode
        {
            get { return _methodeTo; }
        }

        /// <summary>
        /// Identifiant interne, pour HookPool, de ce remplacement de méthode managée
        /// </summary>
        public uint NumHook
        {
            get { return _numHook; }
        }

        /// <summary>
        /// Indique si cette instance gère une décoration de méthode (une méthode exécutée avant et/ou une après)<br/>
        /// Sinon, c'est un remplacement de méthode
        /// </summary>
        public bool EstMethodesDecoration
        {
            get { return _estMethodeDecoration; }
        }

        /// <summary>
        /// Si cette instance gère une décoration de méthode, indique la méthode exécutée avant
        /// </summary>
        public MethodInfo MethodeAvant
        {
            get { return _methodeAvant; }
        }

        /// <summary>
        /// Si cette instance gère une décoration de méthode, indique la méthode exécutée après
        /// </summary>
        public MethodInfo MethodeApres
        {
            get { return _methodeApres; }
        }

        /// <summary>
        /// Classe gérant le remplacement d'une méthode managée<br/>
        /// De vos projets/vos librairies, qui ne sont pas dans le GAC
        /// </summary>
        internal ManagedHook(uint numHook, MethodInfo methodeFrom, MethodInfo methodeTo, bool autoActive = true)
        {
            _numHook = numHook;
            _methodeFrom = methodeFrom;
            _methodeTo = methodeTo;

            Prepare(autoActive);
        }

        /// <summary>
        /// Classe gérant la décoration d'une méthode managée (Exécutant une méthode avant et/ou une méthode après)<br/>
        /// de vos projets/vos librairies, qui ne sont pas dans le GAC
        /// </summary>
        internal ManagedHook(uint numHook, MethodInfo methodeFrom, MethodInfo methodeAvant, MethodInfo methodeApres, bool autoActive = true)
        {
            _numHook = numHook;
            _methodeFrom = methodeFrom;
            _methodeAvant = methodeAvant;
            _methodeApres = methodeApres;

            _estMethodeDecoration = true;

            Prepare(autoActive);
        }

        internal ManagedHook(uint numHook, ConstructorInfo constructeur, MethodInfo methodeDeRemplacement, bool autoActive = true)
        {
            _numHook = numHook;
            _constructeur = constructeur;
            _methodeTo = methodeDeRemplacement;

            Prepare(autoActive);
        }

        internal ManagedHook(uint numHook, ConstructorInfo constructeur, MethodInfo methodeAvant, MethodInfo methodeApres, bool autoActive = true)
        {
            _numHook = numHook;
            _constructeur = constructeur;
            _methodeAvant = methodeAvant;
            _methodeApres = methodeApres;

            _estMethodeDecoration = true;

            Prepare(autoActive);
        }

        private void Prepare(bool autoActiver)
        {
            if (_estMethodeDecoration)
            {
                if (EstConstructeur)
                {
                    RuntimeHelpers.PrepareMethod(_constructeur.MethodHandle);
                    if (_methodeAvant != null)
                        RuntimeHelpers.PrepareMethod(_methodeAvant.MethodHandle);
                    if (_methodeApres != null)
                        RuntimeHelpers.PrepareMethod(_methodeApres.MethodHandle);
                }
                else
                {
                    RuntimeHelpers.PrepareMethod(_methodeFrom.MethodHandle);
                    if (_methodeAvant != null)
                        RuntimeHelpers.PrepareMethod(_methodeAvant.MethodHandle);
                    if (_methodeApres != null)
                        RuntimeHelpers.PrepareMethod(_methodeApres.MethodHandle);
                }
            }
            else if (EstConstructeur)
            {
                RuntimeHelpers.PrepareMethod(_methodeTo.MethodHandle);
                RuntimeHelpers.PrepareMethod(_constructeur.MethodHandle);
            }
            else
            {
                RuntimeHelpers.PrepareMethod(_methodeFrom.MethodHandle);
                RuntimeHelpers.PrepareMethod(_methodeTo.MethodHandle);
            }

            if (Debugger.IsAttached)
                ConstruireManagedMethode();
            else
                ConstruireMethodeBuilder();

            if (EstConstructeur)
            {
                _methodePasserelle = _maClasseDynamique.GetType().GetMethod($"{HookPool.NOM_METHODE}{_numHook}", BindingFlags.NonPublic | BindingFlags.Static);
                RuntimeHelpers.PrepareMethod(_methodePasserelle.MethodHandle);

                IntPtr ptrPasserelle;
                if (Debugger.IsAttached)
                {
                    if (IntPtr.Size == 4)
                    {
                        _ptrConstructeur = new IntPtr((int)_constructeur.MethodHandle.GetFunctionPointer() + Marshal.ReadInt32(_constructeur.MethodHandle.GetFunctionPointer() + 1) + 5);
                        ptrPasserelle = new IntPtr((int)_methodePasserelle.MethodHandle.GetFunctionPointer() + Marshal.ReadInt32(_methodePasserelle.MethodHandle.GetFunctionPointer() + 1) + 5);
                    }
                    else
                    {
                        _ptrConstructeur = new IntPtr((long)_constructeur.MethodHandle.GetFunctionPointer() + Marshal.ReadInt32(_constructeur.MethodHandle.GetFunctionPointer() + 1) + 5);
                        ptrPasserelle = new IntPtr((long)_methodePasserelle.MethodHandle.GetFunctionPointer() + Marshal.ReadInt32(_methodePasserelle.MethodHandle.GetFunctionPointer() + 1) + 5);
                    }
                }
                else
                {
                    _ptrConstructeur = _constructeur.MethodHandle.GetFunctionPointer();
                    ptrPasserelle = _methodePasserelle.MethodHandle.GetFunctionPointer();
                }

                _copyParente = new DynamicMethod("Constructeur_" + _numHook.ToString(), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(object), _constructeur.GetParameters()?.Select(pi => pi.ParameterType).ToArray(), _constructeur.DeclaringType, false);
                ILGenerator ilGen = _copyParente.GetILGenerator(_constructeur.GetMethodBody().GetILAsByteArray().Length);
                if (_constructeur.GetMethodBody().LocalVariables != null && _constructeur.GetMethodBody().LocalVariables.Count > 0)
                    foreach (LocalVariableInfo lv in _constructeur.GetMethodBody().LocalVariables)
                        ilGen.DeclareLocal(lv.LocalType, lv.IsPinned);
                if (_constructeur.GetMethodBody().GetILAsByteArray() != null && _constructeur.GetMethodBody().GetILAsByteArray().Length > 0)
                {
                    GCHandle gC1 = GCHandle.Alloc(_copyParente.GetMethodBody().GetILAsByteArray(), GCHandleType.Pinned);
                    IntPtr ptrDestination = Marshal.UnsafeAddrOfPinnedArrayElement(_copyParente.GetMethodBody().GetILAsByteArray(), 0);
                    Marshal.Copy(_constructeur.GetMethodBody().GetILAsByteArray(), 0, gC1.AddrOfPinnedObject(), _constructeur.GetMethodBody().GetILAsByteArray().Length);
                    gC1.Free();
                }

                _nouveauPtr = (uint)(int)((long)ptrPasserelle - ((long)_ptrConstructeur + 1 + sizeof(uint)));

                if (!WinAPI.VirtualProtect(_ptrConstructeur, (IntPtr)5, 0x40, out _))
                    throw new Exceptions.ErreurRePaginationMemoireException(_ptrConstructeur);

                _opOriginal = Marshal.ReadByte(_ptrConstructeur);
                _ptrOriginal = Marshal.ReadInt32(_ptrConstructeur + 1);
            }
            else
            {
                _methodePasserelle = _maClasseDynamique.GetType().GetMethod($"{HookPool.NOM_METHODE}{_numHook}", BindingFlags.NonPublic | BindingFlags.Static);
                RuntimeHelpers.PrepareMethod(_methodePasserelle.MethodHandle);

                _methodeParente = _maClasseDynamique.GetType().GetMethod($"{HookPool.NOM_METHODE_PARENTE}{_numHook}", BindingFlags.NonPublic | BindingFlags.Static);
                RuntimeHelpers.PrepareMethod(_methodeParente.MethodHandle);
            }

            if (autoActiver)
                Active();
        }

        private Type TypeDeRetour
        {
            get
            {
                if (_methodeFrom != null)
                    return _methodeFrom.ReturnType;
                else if (_constructeur != null)
                    return _constructeur.DeclaringType;
                else
                    return typeof(void);
            }
        }

        private ParameterInfo[] ParametresMethode
        {
            get
            {
                if (_methodeFrom != null)
                    return _methodeFrom.GetParameters();
                else if (_constructeur != null)
                    return _constructeur.GetParameters();
                else
                    return new ParameterInfo[] { };
            }
        }

        private bool EstStatic
        {
            get
            {
                if (_methodeFrom != null)
                    return _methodeFrom.IsStatic;
                else if (_constructeur != null)
                    return _constructeur.IsStatic;
                else
                    return true;
            }
        }

        /// <summary>
        /// Active le remplacement de la méthode managée (appelle la nouvelle méthode à la place)
        /// </summary>
        public void Active()
        {
            lock (_threadSafe)
            {
                if (!_actif)
                {
                    if (EstConstructeur)
                    {
                        Marshal.WriteByte(_ptrConstructeur, 0xE9);
                        Marshal.WriteInt32(_ptrConstructeur + 1, (int)_nouveauPtr);
                    }
                    else
                    {
                        _methodeParente.RemplaceMethodeManagee(_methodeFrom);
                        _methodeFrom.RemplaceMethodeManagee(_methodePasserelle);
                    }

                    _actif = true;
                }
            }
        }

        /// <summary>
        /// Désactive le remplacement de la méthode managée (appel la méthode d'origine)
        /// </summary>
        public void Desactive()
        {
            lock (_threadSafe)
            {
                if (_actif)
                {
                    if (EstConstructeur)
                    {
                        
                        Marshal.WriteByte(_ptrConstructeur, _opOriginal);
                        Marshal.WriteInt32(_ptrConstructeur + 1, _ptrOriginal);
                    }
                    else
                        _methodeFrom.RemplaceMethodeManagee(_methodeParente);

                    _actif = false;
                }
            }
        }

        /// <summary>
        /// Retourne si oui ou non cette substitution est sur un constructeur
        /// </summary>
        public bool EstConstructeur
        {
            get { return _constructeur != null; }
        }

        /// <summary>
        /// Appel la méthode/le constructeur d'origine
        /// </summary>
        /// <param name="instance">Instance de l'objet d'origine (null si méthode static)</param>
        /// <param name="args">Arguments de la méthode d'origine</param>
        /// <remarks>Dans le cas d'un constructeur, l'opération n'est pas multi-thread</remarks>
        public object AppelMethodeParente(object instance = null, params object[] args)
        {
            if (EstConstructeur)
            {
                lock (_threadSafe)
                {
                    try
                    {
                        Desactive();
                        if ((args == null && _constructeur.GetParameters().Length > 0) || (args != null && args.Length < _constructeur.GetParameters().Length))
                            for (int i = args?.Length ?? 0; i < _constructeur.GetParameters().Length + 1; i++)
                                args = args.Concat(null).ToArray();
                        return _constructeur.Invoke(instance, ((args == null || args.Length == 0) ? null : args));
                    }
                    finally
                    {
                        Active();
                    }
                }
            }
            else
            {
                if (!EstStatic)
                    if (instance == null)
                        throw new ArgumentNullException(nameof(instance));
                    else if (args != null && args.Length > 0)
                        args = new object[] { instance }.Concat(args).ToArray();
                    else
                        args = new object[] { instance };

                if (args == null && args.Length < _methodeFrom.GetParameters().Length)
                    for (int i = args.Length; i < _methodeFrom.GetParameters().Length + 1; i++)
                        args = args.Concat(null).ToArray();

                if (TypeDeRetour == typeof(void))
                {
                    _methodeParente.Invoke(null, (args.Length == 0 ? null : args));
                    return null;
                }
                else
                    return _methodeParente.Invoke(null, (args.Length == 0 ? null : args));
            }
        }

        #region Version MethodBuilder

        private void ConstruireMethodeBuilder()
        {
            TypeBuilder tb = HookPool.GetInstance().Constructeur(_numHook);
            ILGenerator ilGen;

            List<Type> listeTypeParametres = new();
            // Si ce n'est pas une static, déjà, on peut ajouter en premier paramètre l'instance de l'objet
            if (!EstStatic)
                listeTypeParametres.Add(typeof(object));
            if (ParametresMethode != null && ParametresMethode.Length > 0)
                foreach (ParameterInfo parameterInfo in ParametresMethode)
                    listeTypeParametres.Add(parameterInfo.ParameterType);

            MethodBuilder mbParente = tb.DefineMethod(HookPool.NOM_METHODE_PARENTE + _numHook.ToString(), MethodAttributes.Private | MethodAttributes.Static, (TypeDeRetour == typeof(void) ? typeof(void) : typeof(object)), listeTypeParametres.ToArray());
            ilGen = mbParente.GetILGenerator();
            ilGen.Emit(OpCodes.Ldstr, "Impossible d'appeler la méthode parente");
            ilGen.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new Type[] { typeof(string) }));
            ilGen.Emit(OpCodes.Throw);

            MethodBuilder mb = tb.DefineMethod(HookPool.NOM_METHODE + _numHook.ToString(), MethodAttributes.Private | MethodAttributes.Static, (TypeDeRetour == typeof(void) ? typeof(void) : typeof(object)), listeTypeParametres.ToArray());
            if (!EstStatic)
            {
                ParameterBuilder pb = mb.DefineParameter(1, ParameterAttributes.Optional, "monThis");
                pb.SetConstant(null);
            }
            ilGen = mb.GetILGenerator(256);

            ilGen.DeclareLocal(typeof(ManagedHook)); // Note : variable local : monHook
            ilGen.DeclareLocal(typeof(List<object>)); // Note : variable local : listeParametres
            ilGen.DeclareLocal(typeof(bool)); // Note : variable local pour les "if"
            if (TypeDeRetour != typeof(void))
            {
                ilGen.DeclareLocal(typeof(object));
            }
            Label label1 = ilGen.DefineLabel();
            Label label2 = ilGen.DefineLabel();
            Label label3 = ilGen.DefineLabel();

            // ManagedHook monHook = HookPool.GetInstance().RetourneHook(<NumHook>); // Note : <NumHook> est un int, définit juste à la ligne suivante (ldc_I4, _numHook) est donc l'identifiant NumHook de cette instance
            ilGen.Emit(OpCodes.Call, typeof(HookPool).GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static));
            ilGen.Emit(OpCodes.Ldc_I4, _numHook);
            ilGen.Emit(OpCodes.Callvirt, typeof(HookPool).GetMethod(nameof(HookPool.RetourneHook), new Type[] { typeof(int) }));
            ilGen.Emit(OpCodes.Stloc_0);
            // List<object> listeParametres = new();
            ilGen.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(new Type[] { }));
            ilGen.Emit(OpCodes.Stloc_1);
            // if (monHook.Actif) // Note : Si faux, goto label1
            ilGen.Emit(OpCodes.Ldloc_0);
            ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(Actif)).GetGetMethod());
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ceq);
            ilGen.Emit(OpCodes.Stloc_2);
            ilGen.Emit(OpCodes.Ldloc_2);
            ilGen.Emit(OpCodes.Brtrue_S, label1);
            if (!EstStatic)
            {
                // listeParametres.Add(monThis); // Note : monThis est le nom du premier paramètre de cette méthode
                ilGen.Emit(OpCodes.Ldloc_1);
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
            }
            if (ParametresMethode != null && ParametresMethode.Length > 0)
            {
                // listeParametres.Add(<Param>); // Note : <Param> est/sont le(s) paramètre(s) supplémentaires de ma méthode
                int numParam = 1;
                foreach (ParameterInfo pi in ParametresMethode)
                {
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Ldarg, numParam++);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
                }
            }

            // Si la méthode destination a plus de paramètre que la méthode source, ont les spécifie, en mettant null
            if (_estMethodeDecoration)
            {
                if (_methodeAvant != null && _methodeAvant.GetParameters().Length - 1 > ParametresMethode.Length)
                {
                    for (int i = ParametresMethode.Length + 1; i <= _methodeAvant.GetParameters().Length - 1; i++)
                    {
                        // listeParametres.Add(null);
                        ilGen.Emit(OpCodes.Ldloc_1);
                        ilGen.Emit(OpCodes.Ldnull);
                        ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
                    }
                }
            }
            else
            {
                if (_methodeTo.GetParameters().Length - 1 > ParametresMethode.Length)
                {
                    for (int i = ParametresMethode.Length + 1; i <= _methodeTo.GetParameters().Length - 1; i++)
                    {
                        // listeParametres.Add(null);
                        ilGen.Emit(OpCodes.Ldloc_1);
                        ilGen.Emit(OpCodes.Ldnull);
                        ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
                    }
                }
            }

            ilGen.MarkLabel(label1);
            // if (monHook.Actif) // Note : Si faux, goto label2
            ilGen.Emit(OpCodes.Ldloc_0);
            ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(Actif)).GetGetMethod());
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ceq);
            ilGen.Emit(OpCodes.Stloc_2);
            ilGen.Emit(OpCodes.Ldloc_2);
            ilGen.Emit(OpCodes.Brtrue_S, label2);
            if (_estMethodeDecoration)
            {
                if (_methodeAvant != null)
                {
                    // monHook.MethodeAvant.Invoke(<Arg>, listeParametres.ToArray()); // Note : Arg est null, si static, sinon contient monThis (premier paramètre de cette méthode)
                    ilGen.Emit(OpCodes.Ldloc_0);
                    ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(MethodeAvant)).GetGetMethod());
                    if (!EstStatic)
                        ilGen.Emit(OpCodes.Ldarg_0);
                    else
                        ilGen.Emit(OpCodes.Ldnull);
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), new Type[] { typeof(object), typeof(object[]) }));
                    ilGen.Emit(OpCodes.Pop);
                }
                // monHook.AppelMethodeParente(<monThis>, listeParametres.ToArray());  // Note : <monThis> si ce n'est pas une méthode static, sinon null
                // ou monHook.AppelMethodeParente(<monThis>, object[]null);     // Si la méthode n'a pas de paramètre
                ilGen.Emit(OpCodes.Ldloc_0);
                if (!EstStatic)
                    ilGen.Emit(OpCodes.Ldarg_0);
                else
                    ilGen.Emit(OpCodes.Ldnull);
                if (ParametresMethode.Length > 0)
                {
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                }
                else
                    ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetMethod(nameof(AppelMethodeParente), BindingFlags.Instance | BindingFlags.Public));
                // Et stock le retour, si la méthode retourne une valeur (et non une méthode type void)
                if (TypeDeRetour != typeof(void))
                    ilGen.Emit(OpCodes.Stloc_3);
                else
                    ilGen.Emit(OpCodes.Pop);
                if (_methodeApres != null)
                {
                    // monHook.MethodeApres.Invoke(<Arg>, listeParametres.ToArray());
                    ilGen.Emit(OpCodes.Ldloc_0);
                    ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(MethodeApres)).GetGetMethod());
                    if (!EstStatic)
                        ilGen.Emit(OpCodes.Ldarg_0);
                    else
                        ilGen.Emit(OpCodes.Ldnull);
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), new Type[] { typeof(object), typeof(object[]) }));
                    ilGen.Emit(OpCodes.Pop);
                }
            }
            else
            {
                // monHook.ToMethode.Invoke(<Arg>, listeParametres.ToArray()); // Note : Arg est null, si static, sinon contient monThis (premier paramètre de cette méthode)
                ilGen.Emit(OpCodes.Ldloc_0);
                ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(ToMethode)).GetGetMethod());
                if (!EstStatic)
                    ilGen.Emit(OpCodes.Ldarg_0);
                else
                    ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Ldloc_1);
                ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), new Type[] { typeof(object), typeof(object[]) }));
                // Et stock le retour, si la méthode retourne une valeur (et non une méthode type void)
                if (TypeDeRetour != typeof(void))
                    ilGen.Emit(OpCodes.Stloc_3);
                else
                    ilGen.Emit(OpCodes.Pop);
            }
            // Note : goto label3 (fin du If, car juste en dessous (label2) = début du Else)
            ilGen.Emit(OpCodes.Br_S, label3);
            ilGen.MarkLabel(label2);
            if (EstConstructeur)
            {
                // monHook.AppelMethodeParente(<monThis>, listeParametres.ToArray());  // Note : <monThis> si ce n'est pas une méthode static, sinon null
                // ou monHook.AppelMethodeParente(<monThis>, object[]null);     // Si la méthode n'a pas de paramètre
                ilGen.Emit(OpCodes.Ldloc_0);
                if (!EstStatic)
                    ilGen.Emit(OpCodes.Ldarg_0);
                else
                    ilGen.Emit(OpCodes.Ldnull);
                if (ParametresMethode.Length > 0)
                {
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                }
                else
                    ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetMethod(nameof(AppelMethodeParente), BindingFlags.Instance | BindingFlags.Public));
                ilGen.Emit(OpCodes.Stloc_3);
            }
            else
            {
                // InvokeParente_<NumHook>(params); // Note, on construit le nom de la méthode en concaténant le numHook. Note 2 : params est la liste des paramètres de la méthode d'origine (remplacée) si elle en a, bien sur
                if (!EstStatic)
                    ilGen.Emit(OpCodes.Ldarg_0);
                if (ParametresMethode.Length > 0)
                {
                    int numParam = 1;
                    foreach (ParameterInfo pi in ParametresMethode)
                    {
                        ilGen.Emit(OpCodes.Ldarg, numParam++);
                    }
                }
                ilGen.Emit(OpCodes.Call, mbParente);
                if (TypeDeRetour != typeof(void))
                    ilGen.Emit(OpCodes.Stloc_3);
            }
            // return <Resultat>; // Note : on retourne le résultat de la méthode précédemment exécutée, si ce n'est pas une void/Sub bien sur
            ilGen.MarkLabel(label3);
            // Fin, on retourne l'objet si ce n'est pas une void
            if (TypeDeRetour != typeof(void))
            {
                ilGen.Emit(OpCodes.Ldloc_3);
            }
            // Note : Marque de fin de la méthode
            ilGen.Emit(OpCodes.Ret);

            Type type = tb.CreateType();
            _maClasseDynamique = Activator.CreateInstance(type);
        }

        #endregion

        #region Version Assembly compilée

        private void ConstruireManagedMethode()
        {
            string corps;
            corps = "";
            corps += $"using {nameof(HookManager)}.{nameof(Modeles)};" + Environment.NewLine;
            corps += "using System.Collections.Generic;" + Environment.NewLine;
            corps += $"namespace {nameof(HookManager)}.Hooks" + Environment.NewLine;
            corps += "{" + Environment.NewLine;
            corps += $"public class {HookPool.NOM_CLASSE}{_numHook}" + Environment.NewLine;
            corps += "{" + Environment.NewLine;

            // Méthode passerelle
            if ((!Debugger.IsAttached) || (!HookPool.GetInstance().ModeDebugInterne))
                corps += "[System.Diagnostics.DebuggerNonUserCode()]" + Environment.NewLine;
            corps += "private static ";
            corps += $"{(TypeDeRetour == typeof(void) ? "void" : "object")} {HookPool.NOM_METHODE}{_numHook}({(EstStatic ? "" : "object monThis = null")}";
            if (ParametresMethode.Length > 0)
                for (int i = 0; i < ParametresMethode.Length; i++)
                {
                    if ((!EstStatic) || (i > 0)) corps += ", ";
                    corps += ParametresMethode[i].ParameterType.ToString() + $" param{(i + 1)} = null";
                }
            corps += ")" + Environment.NewLine;
            corps += "{" + Environment.NewLine;
            corps += $"{nameof(ManagedHook)} myHook = {nameof(HookPool)}.{nameof(HookPool.GetInstance)}().{nameof(HookPool.RetourneHook)}({_numHook});" + Environment.NewLine;
            corps += $"List<object> param = new List<object>();" + Environment.NewLine;
            corps += "if (myHook.Actif)" + Environment.NewLine;
            corps += "{" + Environment.NewLine;
            if (!EstStatic) corps += "param.Add(monThis);" + Environment.NewLine;
            corps += "}" + Environment.NewLine;

            if (ParametresMethode.Length > 0)
                for (int i = 0; i < ParametresMethode.Length; i++)
                    corps += $"param.Add(param{(i + 1)});" + Environment.NewLine;

            if (_estMethodeDecoration)
            {
                if (_methodeAvant != null && _methodeAvant.GetParameters().Length - (EstStatic ? 0 : 1) > ParametresMethode.Length)
                    for (int i = 0; i < _methodeAvant.GetParameters().Length - (EstStatic ? 0 : 1) - ParametresMethode.Length; i++)
                        corps += "param.Add(null);" + Environment.NewLine;
            }
            else
                if (_methodeTo.GetParameters().Length - (EstStatic ? 0 : 1) > ParametresMethode.Length)
                    for (int i = 0; i < _methodeTo.GetParameters().Length - (EstStatic ? 0 : 1) - ParametresMethode.Length; i++)
                        corps += "param.Add(null);" + Environment.NewLine;

            corps += "if (myHook.Actif)" + Environment.NewLine;
            corps += "{" + Environment.NewLine;
            if (_estMethodeDecoration)
            {
                if (_methodeAvant != null)
                    corps += $"myHook.{nameof(MethodeAvant)}.Invoke({(EstStatic ? "null" : "monThis")}, param.ToArray());" + Environment.NewLine;
                if (TypeDeRetour != typeof(void))
                    corps += "object retour = ";
                corps += $"myHook.AppelMethodeParente({(EstStatic ? "null" : "monThis")}, {(ParametresMethode.Length > 0 ? ", param.ToArray()" : "null")});" + Environment.NewLine;
                if (_methodeApres != null)
                {
                    corps += $"myHook.{nameof(MethodeApres)}.Invoke({(EstStatic ? "null" : "monThis")}, param.ToArray());" + Environment.NewLine;
                }
                if (TypeDeRetour != typeof(void))
                    corps += "return retour;" + Environment.NewLine;
            }
            else
            {
                if (TypeDeRetour != typeof(void))
                    corps += "return ";
                corps += $"myHook.{nameof(ToMethode)}.Invoke({(EstStatic ? "null" : "monThis")}, param.ToArray());" + Environment.NewLine;
            }
            corps += "}" + Environment.NewLine;
            corps += "else" + Environment.NewLine;
            corps += "{" + Environment.NewLine;
            if (TypeDeRetour != typeof(void))
                corps += "return ";
            if (EstConstructeur)
            {
                corps += $"myHook.AppelMethodeParente";
            }
            else
            {
                corps += $"{HookPool.NOM_METHODE_PARENTE}{_numHook}";
            }
            corps += $"({(EstStatic && !EstConstructeur ? "" : "monThis")}";
            if (ParametresMethode.Length > 0)
                for (int i = 0; i < ParametresMethode.Length; i++)
                {
                    if ((i > 0) || (!EstStatic || EstConstructeur)) corps += ", ";
                    corps += $"param{(i + 1)}";
                }
            corps += ");" + Environment.NewLine;
            corps += "}" + Environment.NewLine;
            corps += "}" + Environment.NewLine;
            corps += Environment.NewLine;

            // Méthode parente
            if ((!Debugger.IsAttached) || (!HookPool.GetInstance().ModeDebugInterne))
                corps += "[System.Diagnostics.DebuggerNonUserCode()]" + Environment.NewLine;
            corps += "private static ";
            corps += $"{(TypeDeRetour == typeof(void) ? "void" : "object")} {HookPool.NOM_METHODE_PARENTE}{_numHook}(";
            if (!EstStatic) corps += "object monThis";
            int j = 0;
            if (ParametresMethode.Length > 0)
                for (j = 0; j < ParametresMethode.Length; j++)
                {
                    if ((j > 0) || (!EstStatic)) corps += ", ";
                    corps += ParametresMethode[j].ParameterType.ToString() + $" param{(j + 1)} = null";
                }
            if ((_estMethodeDecoration) && (TypeDeRetour != typeof(void)) && !EstConstructeur)
                corps += $", object param{j + 1} = null";

            corps += ")" + Environment.NewLine;
            corps += "{" + Environment.NewLine;
            corps += @"throw new System.Exception(""Impossible d'appeler la méthode parente"");" + Environment.NewLine;
            corps += "}" + Environment.NewLine;

            corps += "}" + Environment.NewLine;
            corps += "}" + Environment.NewLine;

            CompilerParameters optionsCompilateur = new()
            {
                GenerateExecutable = false,
                GenerateInMemory = !HookPool.GetInstance().ModeDebugInterne,
                CompilerOptions = "/t:library -debug -optimize- -platform:anycpu",
                TreatWarningsAsErrors = false,
                IncludeDebugInformation = HookPool.GetInstance().ModeDebugInterne,
            };
            optionsCompilateur.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);

            CompilerResults retour = HookPool.GetInstance().Compilateur().CompileAssemblyFromSource(optionsCompilateur, corps);
            if (retour.Errors.Count == 0)
            {
                _maClasseDynamique = retour.CompiledAssembly.CreateInstance($"{nameof(HookManager)}.Hooks.{HookPool.NOM_CLASSE}{_numHook}");
                if ((Debugger.IsAttached) && (HookPool.GetInstance().ModeDebugInterne))
                    System.IO.File.AppendAllText(retour.TempFiles.BasePath + ".0.cs", corps);
            }
            else
                throw new Exception(retour.Errors[0].ErrorText);
        }

        #endregion
    }
}
