using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using HookManager.Exceptions;
using HookManager.Helpers;

namespace HookManager.Modeles
{
    /// <summary>
    /// Class manage one replaced/decorated method/property/constructor<br/>
    /// Managed and not in GAC only
    /// </summary>
    public sealed class ManagedHook
    {
        private readonly uint _numHook;
        private readonly MethodInfo _methodFrom;
        private readonly MethodInfo _methodTo;
        private MethodInfo _methodGateway;
        private object _myDynamicClass;
        private MethodInfo _parentMethod;
        private bool _enabled;
        private readonly ConstructorInfo _constructor;

        private readonly bool _isDecorativeMethod;
        private readonly MethodInfo _methodBefore;
        private readonly MethodInfo _methodAfter;

        private byte _opOriginal;
        private int _ptrOriginal;
        private uint _newPtr;
        private IntPtr _constructorPtr;
        private DynamicMethod _copyParent;

        private readonly object _threadSafe = new();

        /// <summary>
        /// Is the hook is active
        /// </summary>
        public bool IsEnabled
        {
            get { return _enabled; }
        }

        /// <summary>
        /// Original method
        /// </summary>
        public MethodInfo FromMethod
        {
            get { return _methodFrom; }
        }

        /// <summary>
        /// New method (replacement)
        /// </summary>
        public MethodInfo ToMethod
        {
            get { return _methodTo; }
        }

        /// <summary>
        /// Internal id, for HookPool, of this hook
        /// </summary>
        public uint InternalNumHook
        {
            get { return _numHook; }
        }

        /// <summary>
        /// Is it a hook to manage a decorate method<br/>
        /// Else, it's a replacement method
        /// </summary>
        public bool IsDecorativeMethods
        {
            get { return _isDecorativeMethod; }
        }

        /// <summary>
        /// If <see cref="IsDecorativeMethods"/> then it's the method to call before
        /// </summary>
        public MethodInfo MethodBefore
        {
            get { return _methodBefore; }
        }

        /// <summary>
        /// If <see cref="IsDecorativeMethods"/> then it's the method to call after
        /// </summary>
        public MethodInfo MethodAfter
        {
            get { return _methodAfter; }
        }

        /// <summary>
        /// Class manage one replaced method/property<br/>
        /// Managed and not in GAC only
        /// </summary>
        internal ManagedHook(uint numHook, MethodInfo methodeFrom, MethodInfo methodeTo, bool autoActive = true)
        {
            _numHook = numHook;
            _methodFrom = methodeFrom;
            _methodTo = methodeTo;

            Prepare(autoActive);
        }

        /// <summary>
        /// Class manage one decorated method/property<br/>
        /// Managed and not in GAC only
        /// </summary>
        internal ManagedHook(uint numHook, MethodInfo methodeFrom, MethodInfo methodeAvant, MethodInfo methodeApres, bool autoActive = true)
        {
            _numHook = numHook;
            _methodFrom = methodeFrom;
            _methodBefore = methodeAvant;
            _methodAfter = methodeApres;

            _isDecorativeMethod = true;

            Prepare(autoActive);
        }

        /// <summary>
        /// Class manage one replaced constructor<br/>
        /// Managed and not in GAC only
        /// </summary>
        internal ManagedHook(uint numHook, ConstructorInfo constructeur, MethodInfo methodeDeRemplacement, bool autoActive = true)
        {
            _numHook = numHook;
            _constructor = constructeur;
            _methodTo = methodeDeRemplacement;

            Prepare(autoActive);
        }

        /// <summary>
        /// Class manage one decorated constructor<br/>
        /// Managed and not in GAC only
        /// </summary>
        internal ManagedHook(uint numHook, ConstructorInfo constructeur, MethodInfo methodeAvant, MethodInfo methodeApres, bool autoActive = true)
        {
            _numHook = numHook;
            _constructor = constructeur;
            _methodBefore = methodeAvant;
            _methodAfter = methodeApres;

            _isDecorativeMethod = true;

            Prepare(autoActive);
        }

        private void Prepare(bool autoEnable)
        {
            if (_isDecorativeMethod)
            {
                if (IsConstructor)
                {
                    RuntimeHelpers.PrepareMethod(_constructor.MethodHandle);
                    if (_methodBefore != null)
                        RuntimeHelpers.PrepareMethod(_methodBefore.MethodHandle);
                    if (_methodAfter != null)
                        RuntimeHelpers.PrepareMethod(_methodAfter.MethodHandle);
                }
                else
                {
                    RuntimeHelpers.PrepareMethod(_methodFrom.MethodHandle);
                    if (_methodBefore != null)
                        RuntimeHelpers.PrepareMethod(_methodBefore.MethodHandle);
                    if (_methodAfter != null)
                        RuntimeHelpers.PrepareMethod(_methodAfter.MethodHandle);
                }
            }
            else if (IsConstructor)
            {
                RuntimeHelpers.PrepareMethod(_methodTo.MethodHandle);
                RuntimeHelpers.PrepareMethod(_constructor.MethodHandle);
            }
            else
            {
                RuntimeHelpers.PrepareMethod(_methodFrom.MethodHandle);
                RuntimeHelpers.PrepareMethod(_methodTo.MethodHandle);
            }

            if (Debugger.IsAttached)
                BuildManagedMethod();
            else
                BuildMethodBuilder();

            if (IsConstructor)
            {
                _methodGateway = _myDynamicClass.GetType().GetMethod($"{HookPool.METHOD_NAME}{_numHook}", BindingFlags.NonPublic | BindingFlags.Static);
                RuntimeHelpers.PrepareMethod(_methodGateway.MethodHandle);

                IntPtr ptrGateway;
                if (Debugger.IsAttached)
                {
                    if (IntPtr.Size == 4)
                    {
                        _constructorPtr = new IntPtr((int)_constructor.MethodHandle.GetFunctionPointer() + Marshal.ReadInt32(_constructor.MethodHandle.GetFunctionPointer() + 1) + 5);
                        ptrGateway = new IntPtr((int)_methodGateway.MethodHandle.GetFunctionPointer() + Marshal.ReadInt32(_methodGateway.MethodHandle.GetFunctionPointer() + 1) + 5);
                    }
                    else
                    {
                        _constructorPtr = new IntPtr((long)_constructor.MethodHandle.GetFunctionPointer() + Marshal.ReadInt32(_constructor.MethodHandle.GetFunctionPointer() + 1) + 5);
                        ptrGateway = new IntPtr((long)_methodGateway.MethodHandle.GetFunctionPointer() + Marshal.ReadInt32(_methodGateway.MethodHandle.GetFunctionPointer() + 1) + 5);
                    }
                }
                else
                {
                    _constructorPtr = _constructor.MethodHandle.GetFunctionPointer();
                    ptrGateway = _methodGateway.MethodHandle.GetFunctionPointer();
                }

                _copyParent = _constructor.CopyMethod();

                _newPtr = (uint)(int)((long)ptrGateway - ((long)_constructorPtr + 1 + sizeof(uint)));

                if (!WinApi.VirtualProtect(_constructorPtr, (IntPtr)5, 0x40, out _))
                    throw new RepaginateMemoryException(_constructorPtr);

                _opOriginal = Marshal.ReadByte(_constructorPtr);
                _ptrOriginal = Marshal.ReadInt32(_constructorPtr + 1);
            }
            else
            {
                _methodGateway = _myDynamicClass.GetType().GetMethod($"{HookPool.METHOD_NAME}{_numHook}", BindingFlags.NonPublic | BindingFlags.Static);
                RuntimeHelpers.PrepareMethod(_methodGateway.MethodHandle);

                _parentMethod = _myDynamicClass.GetType().GetMethod($"{HookPool.PARENT_METHOD_NAME}{_numHook}", BindingFlags.NonPublic | BindingFlags.Static);
                RuntimeHelpers.PrepareMethod(_parentMethod.MethodHandle);
            }

            if (autoEnable)
                Enable();
        }

        private Type ReturnType
        {
            get
            {
                if (_methodFrom != null)
                    return _methodFrom.ReturnType;
                else if (_constructor != null)
                    return _constructor.DeclaringType;
                else
                    return typeof(void);
            }
        }

        private ParameterInfo[] MethodParameters
        {
            get
            {
                if (_methodFrom != null)
                    return _methodFrom.GetParameters();
                else if (_constructor != null)
                    return _constructor.GetParameters();
                else
                    return [];
            }
        }

        private bool IsStatic
        {
            get
            {
                if (_methodFrom != null)
                    return _methodFrom.IsStatic;
                else if (_constructor != null)
                    return _constructor.IsStatic;
                else
                    return true;
            }
        }

        /// <summary>
        /// Active this hook (enable the replacement or the decorate of this hook)
        /// </summary>
        public void Enable()
        {
            lock (_threadSafe)
            {
                if (!_enabled)
                {
                    if (IsConstructor)
                    {
                        Marshal.WriteByte(_constructorPtr, 0xE9);
                        Marshal.WriteInt32(_constructorPtr + 1, (int)_newPtr);
                    }
                    else
                    {
                        _parentMethod.ReplaceManagedMethod(_methodFrom);
                        _methodFrom.ReplaceManagedMethod(_methodGateway);
                    }

                    _enabled = true;
                }
            }
        }

        /// <summary>
        /// Disable this hook (disable the replacement or the decorate of this hook)
        /// </summary>
        public void Disable()
        {
            lock (_threadSafe)
            {
                if (_enabled)
                {
                    if (IsConstructor)
                    {
                        Marshal.WriteByte(_constructorPtr, _opOriginal);
                        Marshal.WriteInt32(_constructorPtr + 1, _ptrOriginal);
                    }
                    else
                        _methodFrom.ReplaceManagedMethod(_parentMethod);

                    _enabled = false;
                }
            }
        }

        /// <summary>
        /// Is this hook is on a constructor
        /// </summary>
        public bool IsConstructor
        {
            get { return _constructor != null; }
        }

        /// <summary>
        /// Call the method (or constructor) replaced (original)
        /// </summary>
        /// <param name="instance">Instance of the current object (null if static method)</param>
        /// <param name="args">Parameters of original method</param>
        /// <remarks>If it's a constructor ; you can't do step by step debug inside (but it is really called)</remarks>
        public object CallOriginalMethod(object instance = null, params object[] args)
        {
            if (IsConstructor)
            {
                if ((args == null && _constructor.GetParameters().Length > 0) || (args != null && args.Length < _constructor.GetParameters().Length))
                    for (int i = args?.Length ?? 0; i < _constructor.GetParameters().Length + 1; i++)
                        args = args.Concat(null).ToArray();
                if (args == null || args.Length == 0)
                    return _copyParent.Invoke(null, new object[] { instance }.ToArray());
                else
                    return _copyParent.Invoke(null, new object[] { instance }.Concat(args).ToArray());
            }
            else
            {
                if (!IsStatic)
                    if (instance == null)
                        throw new ArgumentNullException(nameof(instance));
                    else if (args != null && args.Length > 0)
                        args = new object[] { instance }.Concat(args).ToArray();
                    else
                        args = [instance];

                if (args != null && args.Length < _methodFrom.GetParameters().Length)
                    for (int i = args.Length; i < _methodFrom.GetParameters().Length + 1; i++)
                        args = args.Concat(null).ToArray();

                if (ReturnType == typeof(void))
                {
                    _parentMethod.Invoke(null, (args == null || args.Length == 0 ? null : args));
                    return null;
                }
                else
                    return _parentMethod.Invoke(null, (args == null || args.Length == 0 ? null : args));
            }
        }

        #region Version MethodBuilder

#pragma warning disable S1144 // Unused private types or members should be removed
        private MethodBuilder CreateMethodBuilderParent()
        {
            TypeBuilder tb = HookPool.GetInstance().Constructor(_numHook);
            ILGenerator ilGen;
            List<Type> listParametersType = [];
            // If it's not a static, we add the instance of object as first parameter
            if (!IsStatic)
                listParametersType.Add(typeof(object));
            if (MethodParameters != null && MethodParameters.Length > 0)
                foreach (ParameterInfo parameterInfo in MethodParameters)
                    listParametersType.Add(parameterInfo.ParameterType);

            MethodBuilder mbParent = tb.DefineMethod(HookPool.PARENT_METHOD_NAME + _numHook.ToString(), MethodAttributes.Private | MethodAttributes.Static, (ReturnType == typeof(void) ? typeof(void) : typeof(object)), listParametersType.ToArray());
            ilGen = mbParent.GetILGenerator();
            ilGen.Emit(OpCodes.Ldstr, "Unable to call parent method");
            ilGen.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor([typeof(string)]));
            ilGen.Emit(OpCodes.Throw);
            tb.CreateType();
            return mbParent;
        }
#pragma warning restore S1144 // Unused private types or members should be removed

        private void BuildMethodBuilder()
        {
            TypeBuilder tb = HookPool.GetInstance().Constructor(_numHook);
            ILGenerator ilGen;

            List<Type> listParametersType = [];
            // If it's not a static, we add the instance of object as first parameter
            if (!IsStatic)
                listParametersType.Add(typeof(object));
            if (MethodParameters != null && MethodParameters.Length > 0)
                foreach (ParameterInfo parameterInfo in MethodParameters)
                    listParametersType.Add(parameterInfo.ParameterType);

            MethodBuilder mbParent = tb.DefineMethod(HookPool.PARENT_METHOD_NAME + _numHook.ToString(), MethodAttributes.Private | MethodAttributes.Static, (ReturnType == typeof(void) ? typeof(void) : typeof(object)), listParametersType.ToArray());
            ilGen = mbParent.GetILGenerator();
            ilGen.Emit(OpCodes.Ldstr, "Unable to call parent method");
            ilGen.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor([typeof(string)]));
            ilGen.Emit(OpCodes.Throw);

            MethodBuilder mb = tb.DefineMethod(HookPool.METHOD_NAME + _numHook.ToString(), MethodAttributes.Private | MethodAttributes.Static, (ReturnType == typeof(void) ? typeof(void) : typeof(object)), listParametersType.ToArray());
            if (!IsStatic)
            {
                ParameterBuilder pb = mb.DefineParameter(1, ParameterAttributes.Optional, "myThis");
                pb.SetConstant(null);
            }
            ilGen = mb.GetILGenerator(256);

            ilGen.DeclareLocal(typeof(ManagedHook)); // Note : local variable : monHook
            ilGen.DeclareLocal(typeof(List<object>)); // Note : local variable : listParameters
            ilGen.DeclareLocal(typeof(bool)); // Note : local variable for "if"
            if (ReturnType != typeof(void))
            {
                ilGen.DeclareLocal(typeof(object));
            }
            Label label1 = ilGen.DefineLabel();
            Label label2 = ilGen.DefineLabel();
            Label label3 = ilGen.DefineLabel();

            // ManagedHook monHook = HookPool.GetInstance().RetourneHook(<NumHook>); // Note : <NumHook> is a int, defined in next line (ldc_I4, _numHook) is, so, the id of this hook
            ilGen.Emit(OpCodes.Call, typeof(HookPool).GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static));
            ilGen.Emit(OpCodes.Ldc_I4, _numHook);
            ilGen.Emit(OpCodes.Callvirt, typeof(HookPool).GetMethod(nameof(HookPool.ReturnHook), [typeof(int)]));
            ilGen.Emit(OpCodes.Stloc_0);
            // List<object> listParameters = new();
            ilGen.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor([]));
            ilGen.Emit(OpCodes.Stloc_1);
            // if (monHook.Actif) // Note : If false, goto label1
            ilGen.Emit(OpCodes.Ldloc_0);
            ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(IsEnabled)).GetGetMethod());
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ceq);
            ilGen.Emit(OpCodes.Stloc_2);
            ilGen.Emit(OpCodes.Ldloc_2);
            ilGen.Emit(OpCodes.Brtrue_S, label1);
            if (!IsStatic)
            {
                // listParameters.Add(myThis); // Note : myThis is the first parameter of this method
                ilGen.Emit(OpCodes.Ldloc_1);
                ilGen.Emit(OpCodes.Ldarg_0);
                ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
            }
            if (MethodParameters != null && MethodParameters.Length > 0)
            {
                // listParameters.Add(<Param>); // Note : <Param> is/are the parameters of this method
                int numParam = 1;
                foreach (ParameterInfo pi in MethodParameters)
                {
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Ldarg, numParam++);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
                }
            }

            // if the replaced method has more parameters than the replaced method, we fill them to null
            if (_isDecorativeMethod)
            {
                if (_methodBefore != null && _methodBefore.GetParameters().Length - 1 > MethodParameters.Length)
                {
                    for (int i = MethodParameters.Length + 1; i <= _methodBefore.GetParameters().Length - 1; i++)
                    {
                        // listParameters.Add(null);
                        ilGen.Emit(OpCodes.Ldloc_1);
                        ilGen.Emit(OpCodes.Ldnull);
                        ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
                    }
                }
            }
            else
            {
                if (_methodTo.GetParameters().Length - 1 > MethodParameters.Length)
                {
                    for (int i = MethodParameters.Length + 1; i <= _methodTo.GetParameters().Length - 1; i++)
                    {
                        // listParameters.Add(null);
                        ilGen.Emit(OpCodes.Ldloc_1);
                        ilGen.Emit(OpCodes.Ldnull);
                        ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
                    }
                }
            }

            ilGen.MarkLabel(label1);
            // if (monHook.Actif) // Note : If false, goto label2
            ilGen.Emit(OpCodes.Ldloc_0);
            ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(IsEnabled)).GetGetMethod());
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ceq);
            ilGen.Emit(OpCodes.Stloc_2);
            ilGen.Emit(OpCodes.Ldloc_2);
            ilGen.Emit(OpCodes.Brtrue_S, label2);
            if (_isDecorativeMethod)
            {
                if (_methodBefore != null)
                {
                    // monHook.MethodeAvant.Invoke(<Arg>, listParameters.ToArray()); // Note : Arg is null, if static, else contains the instance of the object (myThis, first parameter of this method)
                    ilGen.Emit(OpCodes.Ldloc_0);
                    ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(MethodBefore)).GetGetMethod());
                    if (!IsStatic)
                        ilGen.Emit(OpCodes.Ldarg_0);
                    else
                        ilGen.Emit(OpCodes.Ldnull);
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), [typeof(object), typeof(object[])]));
                    ilGen.Emit(OpCodes.Pop);
                }
                // monHook.AppelMethodeOriginale(<myThis>, listParameters.ToArray());  // Note : set <myThis> if it's not a static method, else <null>
                // ou monHook.AppelMethodeOriginale(<myThis>, object[]null);     // If the method has no parameter
                ilGen.Emit(OpCodes.Ldloc_0);
                if (!IsStatic)
                    ilGen.Emit(OpCodes.Ldarg_0);
                else
                    ilGen.Emit(OpCodes.Ldnull);
                if (MethodParameters.Length > 0)
                {
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                }
                else
                    ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetMethod(nameof(CallOriginalMethod), BindingFlags.Instance | BindingFlags.Public));
                // We keep the return of method, if the original method return a value (is not a void)
                if (ReturnType != typeof(void))
                    ilGen.Emit(OpCodes.Stloc_3);
                else
                    ilGen.Emit(OpCodes.Pop);
                if (_methodAfter != null)
                {
                    // monHook.MethodeApres.Invoke(<Arg>, listParameters.ToArray());
                    ilGen.Emit(OpCodes.Ldloc_0);
                    ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(MethodAfter)).GetGetMethod());
                    if (!IsStatic)
                        ilGen.Emit(OpCodes.Ldarg_0);
                    else
                        ilGen.Emit(OpCodes.Ldnull);
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), [typeof(object), typeof(object[])]));
                    ilGen.Emit(OpCodes.Pop);
                }
            }
            else
            {
                // monHook.ToMethode.Invoke(<Arg>, listParameters.ToArray()); // Note : Arg is null if static, else fill to myThis (first parameter of this method, the instance of the object)
                ilGen.Emit(OpCodes.Ldloc_0);
                ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(ToMethod)).GetGetMethod());
                if (!IsStatic)
                    ilGen.Emit(OpCodes.Ldarg_0);
                else
                    ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Ldloc_1);
                ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), [typeof(object), typeof(object[])]));
                // We keep the return of method, if the original method return a value (is not a void)
                if (ReturnType != typeof(void))
                    ilGen.Emit(OpCodes.Stloc_3);
                else
                    ilGen.Emit(OpCodes.Pop);
            }
            // Note : goto label3 (end if, start of Else (label2))
            ilGen.Emit(OpCodes.Br_S, label3);
            ilGen.MarkLabel(label2);
            if (IsConstructor)
            {
                // monHook.AppelMethodeOriginale(<myThis>, listParameters.ToArray());  // Note : set <myThis> if it's not a static method, else <null>
                // or monHook.AppelMethodeOriginale(<myThis>, object[]null);     // If the method has no parameter
                ilGen.Emit(OpCodes.Ldloc_0);
                if (!IsStatic)
                    ilGen.Emit(OpCodes.Ldarg_0);
                else
                    ilGen.Emit(OpCodes.Ldnull);
                if (MethodParameters.Length > 0)
                {
                    ilGen.Emit(OpCodes.Ldloc_1);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                }
                else
                    ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetMethod(nameof(CallOriginalMethod), BindingFlags.Instance | BindingFlags.Public));
                ilGen.Emit(OpCodes.Stloc_3);
            }
            else
            {
                // InvokeParente_<NumHook>(params); // Note, we build the name of the method by concat with numHook (id of the hook). Note 2 : params is the list of parameters of the original methode (replaced) if there is/are
                if (!IsStatic)
                    ilGen.Emit(OpCodes.Ldarg_0);
                if (MethodParameters.Length > 0)
                {
                    int numParam = 1;
                    foreach (ParameterInfo pi in MethodParameters)
                    {
                        ilGen.Emit(OpCodes.Ldarg, numParam++);
                    }
                }
                ilGen.Emit(OpCodes.Call, mbParent);
                if (ReturnType != typeof(void))
                    ilGen.Emit(OpCodes.Stloc_3);
            }
            // return <Resultat>; // Note : we return the result previously keep, if not a void
            ilGen.MarkLabel(label3);
            // End, we return the result previously keep, if not a void
            if (ReturnType != typeof(void))
            {
                ilGen.Emit(OpCodes.Ldloc_3);
            }
            // Note : End of method
            ilGen.Emit(OpCodes.Ret);

            Type type = tb.CreateType();
            _myDynamicClass = Activator.CreateInstance(type);
        }

        #endregion

        #region Version Assembly compilée

        private void BuildManagedMethod()
        {
            StringBuilder corps = new();
            corps.AppendLine($"using {nameof(HookManager)}.{nameof(Modeles)};");
            corps.AppendLine("using System.Collections.Generic;");
            corps.AppendLine($"namespace {nameof(HookManager)}.Hooks");
            corps.AppendLine("{");
            corps.AppendLine($"public class {HookPool.CLASS_NAME}{_numHook}");
            corps.AppendLine("{");

            // Méthode passerelle
            if ((!Debugger.IsAttached) || (!HookPool.GetInstance().ModeInternalDebug))
                corps.AppendLine("[System.Diagnostics.DebuggerNonUserCode()]");
            corps.Append("private static ");
            corps.Append($"{(ReturnType == typeof(void) ? "void" : "object")} {HookPool.METHOD_NAME}{_numHook}({(IsStatic ? "" : "object myThis = null")}");
            if (MethodParameters.Length > 0)
                for (int i = 0; i < MethodParameters.Length; i++)
                {
                    if ((!IsStatic) || (i > 0))
                        corps.Append(", ");
                    corps.Append(MethodParameters[i].ParameterType.ToString().Replace("+", ".") + $" param{(i + 1)} = null");
                }
            corps.AppendLine(")");
            corps.AppendLine("{");
            corps.AppendLine($"{nameof(ManagedHook)} myHook = {nameof(HookPool)}.{nameof(HookPool.GetInstance)}().{nameof(HookPool.ReturnHook)}({_numHook});");
            corps.AppendLine($"List<object> param = new List<object>();");
            corps.AppendLine($"if (myHook.{nameof(IsEnabled)})");
            corps.AppendLine("{");
            if (!IsStatic)
                corps.AppendLine("param.Add(myThis);");
            corps.AppendLine("}");

            if (MethodParameters.Length > 0)
                for (int i = 0; i < MethodParameters.Length; i++)
                    corps.AppendLine($"param.Add(param{(i + 1)});");

            if (_isDecorativeMethod)
            {
                if (_methodBefore != null && _methodBefore.GetParameters().Length - (IsStatic ? 0 : 1) > MethodParameters.Length)
                    for (int i = 0; i < _methodBefore.GetParameters().Length - (IsStatic ? 0 : 1) - MethodParameters.Length; i++)
                        corps.AppendLine("param.Add(null);");
            }
            else if (_methodTo.GetParameters().Length - (IsStatic ? 0 : 1) > MethodParameters.Length)
                for (int i = 0; i < _methodTo.GetParameters().Length - (IsStatic ? 0 : 1) - MethodParameters.Length; i++)
                    corps.AppendLine("param.Add(null);");

            corps.AppendLine($"if (myHook.{nameof(IsEnabled)})");
            corps.AppendLine("{");
            if (_isDecorativeMethod)
            {
                if (_methodBefore != null)
                    corps.AppendLine($"myHook.{nameof(MethodBefore)}.Invoke({(IsStatic ? "null" : "myThis")}, param.ToArray());");
                if (ReturnType != typeof(void))
                    corps.Append("object ret = ");
                corps.AppendLine($"myHook.{nameof(CallOriginalMethod)}({(IsStatic ? "null" : "myThis")}, {(MethodParameters.Length > 0 ? ", param.ToArray()" : "null")});");
                if (_methodAfter != null)
                {
                    corps.AppendLine($"myHook.{nameof(MethodAfter)}.Invoke({(IsStatic ? "null" : "myThis")}, param.ToArray());");
                }
                if (ReturnType != typeof(void))
                    corps.AppendLine("return ret;");
            }
            else
            {
                if (ReturnType != typeof(void))
                    corps.Append("return ");
                corps.AppendLine($"myHook.{nameof(ToMethod)}.Invoke({(IsStatic ? "null" : "myThis")}, param.ToArray());");
            }
            corps.AppendLine("}");
            corps.AppendLine("else");
            corps.AppendLine("{");
            if (ReturnType != typeof(void))
                corps.Append("return ");
            if (IsConstructor)
            {
                corps.Append($"myHook.{nameof(CallOriginalMethod)}");
            }
            else
            {
                corps.Append($"{HookPool.PARENT_METHOD_NAME}{_numHook}");
            }
            corps.Append($"({(IsStatic && !IsConstructor ? "" : "myThis")}");
            if (MethodParameters.Length > 0)
                for (int i = 0; i < MethodParameters.Length; i++)
                {
                    if ((i > 0) || (!IsStatic || IsConstructor))
                        corps.Append(", ");
                    corps.Append($"param{(i + 1)}");
                }
            corps.AppendLine(");");
            corps.AppendLine("}");
            corps.AppendLine("}");
            corps.AppendLine();

            // Méthode parente
            if ((!Debugger.IsAttached) || (!HookPool.GetInstance().ModeInternalDebug))
                corps.AppendLine("[System.Diagnostics.DebuggerNonUserCode()]");
            corps.Append("private static ");
            corps.Append($"{(ReturnType == typeof(void) ? "void" : "object")} {HookPool.PARENT_METHOD_NAME}{_numHook}(");
            if (!IsStatic)
                corps.Append("object myThis");
            int j = 0;
            if (MethodParameters.Length > 0)
                for (j = 0; j < MethodParameters.Length; j++)
                {
                    if ((j > 0) || (!IsStatic))
                        corps.Append(", ");
                    corps.Append(MethodParameters[j].ParameterType.ToString().Replace("+", ".") + $" param{(j + 1)} = null");
                }
            if ((_isDecorativeMethod) && (ReturnType != typeof(void)) && !IsConstructor)
                corps.Append($", object param{j + 1} = null");

            corps.AppendLine(")");
            corps.AppendLine("{");
            corps.AppendLine(@"throw new System.Exception(""Unable to call the original method"");");
            corps.AppendLine("}");

            corps.AppendLine("}");
            corps.AppendLine("}");

            CompilerParameters optionsCompilateur = new()
            {
                GenerateExecutable = false,
                GenerateInMemory = !HookPool.GetInstance().ModeInternalDebug,
                CompilerOptions = "/t:library -debug -optimize- -platform:anycpu",
                TreatWarningsAsErrors = false,
                IncludeDebugInformation = HookPool.GetInstance().ModeInternalDebug,
            };
            if (Debugger.IsAttached && HookPool.GetInstance().ModeInternalDebug)
                optionsCompilateur.TempFiles = new TempFileCollection(System.IO.Path.GetTempPath(), true);

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                if (!asm.IsDynamic && !asm.GlobalAssemblyCache && !string.IsNullOrWhiteSpace(asm.Location))
                    optionsCompilateur.ReferencedAssemblies.Add(asm.Location);

            CompilerResults retour = HookPool.GetInstance().Compiler().CompileAssemblyFromSource(optionsCompilateur, corps.ToString());
            if (retour.Errors.Count == 0)
                _myDynamicClass = retour.CompiledAssembly.CreateInstance($"{nameof(HookManager)}.Hooks.{HookPool.CLASS_NAME}{_numHook}");
            else
                throw new CommonHookManagerException(retour.Errors[0].ErrorText);
        }

        #endregion
    }
}
