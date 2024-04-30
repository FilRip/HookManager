using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using HookManagerCore.Exceptions;
using HookManagerCore.Helpers;
using HookManagerCore.Modeles;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace HookManagerCore.Modeles
{
    /// <summary>
    /// Class manage one replaced/decorated method/property/constructor<br/>
    /// Managed and not in GAC only
    /// </summary>
    public sealed class ManagedHook
    {
        private readonly uint _numHook;
        private readonly MethodBase _methodFrom;
        private readonly MethodInfo _methodTo;
        private MethodInfo _methodGateway;
        private object _myDynamicClass;
        private MethodInfo _parentMethod;
        private bool _enabled;

        private readonly bool _isDecorativeMethod;
        private readonly MethodInfo _methodBefore;
        private readonly MethodInfo _methodAfter;

        /*private byte _opOriginal;
        private int _ptrOriginal;*/

        private readonly object _threadSafe = new();

        /// <summary>
        /// Is the hook is active
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                lock (_threadSafe)
                    _enabled = value;
            }
        }

        /// <summary>
        /// Original method
        /// </summary>
        public MethodBase FromMethod
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
            _methodFrom = constructeur;
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
            _methodFrom = constructeur;
            _methodBefore = methodeAvant;
            _methodAfter = methodeApres;

            _isDecorativeMethod = true;

            Prepare(autoActive);
        }

        private void Prepare(bool autoEnable)
        {
            if (_isDecorativeMethod)
            {
                RuntimeHelpers.PrepareMethod(_methodFrom.MethodHandle);
                if (_methodBefore != null)
                    RuntimeHelpers.PrepareMethod(_methodBefore.MethodHandle);
                if (_methodAfter != null)
                    RuntimeHelpers.PrepareMethod(_methodAfter.MethodHandle);
            }
            else
            {
                RuntimeHelpers.PrepareMethod(_methodFrom.MethodHandle);
                RuntimeHelpers.PrepareMethod(_methodTo.MethodHandle);
            }

            if (Debugger.IsAttached)
                BuildManagedMethod();
            else
                BuildMethodBuilder();// Faster but no debug step by step inside generated method

            _methodGateway = _myDynamicClass.GetType().GetMethod($"{HookPool.METHOD_NAME}{_numHook}", BindingFlags.NonPublic | BindingFlags.Static);
            RuntimeHelpers.PrepareMethod(_methodGateway.MethodHandle);

            _parentMethod = _methodFrom.GetOriginalMethod();
            _methodFrom.ReplaceManagedMethod(_methodGateway);

            Enabled = autoEnable;
        }

        private Type ReturnType
        {
            get
            {
                if (_methodFrom is MethodInfo mi)
                    return mi.ReturnType;
                else if (_methodFrom is ConstructorInfo ci)
                    return ci.DeclaringType;
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
                else
                    return true;
            }
        }

        /// <summary>
        /// Is this hook is on a constructor
        /// </summary>
        public bool IsConstructor
        {
            get { return _methodFrom is ConstructorInfo; }
        }

        /// <summary>
        /// Appel la méthode/le constructeur d'origine
        /// </summary>
        /// <param name="instance">Instance de l'objet d'origine (null si méthode static)</param>
        /// <param name="args">Paramètres pour la méthode d'origine, si elle en a</param>
        /// <remarks>Si c'est un constructeur qui a été substitué, vous ne pouvez pas faire de pas à pas dans le constructeur d'origine (mais il est bel et bien appelé)</remarks>
        public object CallOriginalMethod(object instance = null, params object[] args)
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

        #region Version MethodBuilder

        // Faster but no debug step by step inside generated method
        private void BuildMethodBuilder()
        {
            TypeBuilder tb = HookPool.GetInstance().Constructor(_numHook);
            ILGenerator ilGen;

            List<Type> listParametersType = [];
            // If it's not a static, we add the instance of object as first parameter
            if (ReturnType != typeof(void))
                listParametersType.Add(Type.GetType(ReturnType.FullName + "&"));
            if (!IsStatic)
                listParametersType.Add(typeof(object));
            if (MethodParameters != null && MethodParameters.Length > 0)
                foreach (ParameterInfo parameterInfo in MethodParameters)
                    listParametersType.Add(parameterInfo.ParameterType);

            MethodBuilder mb = tb.DefineMethod(HookPool.METHOD_NAME + _numHook.ToString(), MethodAttributes.Private | MethodAttributes.Static, typeof(bool), listParametersType.ToArray());
            ParameterBuilder returnValue = null;
            if (ReturnType != typeof(void))
            {
                returnValue = mb.DefineParameter(1, ParameterAttributes.In, "__result");
            }
            ParameterBuilder pbInstance = null;
            if (!IsStatic)
            {
                pbInstance = mb.DefineParameter((returnValue != null ? 2 : 1), ParameterAttributes.Optional, "__instance");
                pbInstance.SetConstant(null);
            }
            ilGen = mb.GetILGenerator(256);

            LocalBuilder myHook = ilGen.DeclareLocal(typeof(ManagedHook)); // Note : local variable : monHook
            LocalBuilder listParameters = ilGen.DeclareLocal(typeof(List<object>)); // Note : local variable : listParameters
            LocalBuilder ifEnabled = ilGen.DeclareLocal(typeof(bool)); // Note : local variable for "if"
            
            Label label1 = ilGen.DefineLabel();
            Label label2 = ilGen.DefineLabel();
            Label label3 = ilGen.DefineLabel();

            // ManagedHook monHook = HookPool.GetInstance().RetourneHook(<NumHook>); // Note : <NumHook> is a int, defined in next line (ldc_I4, _numHook) is, so, the id of this hook
            ilGen.Emit(OpCodes.Call, typeof(HookPool).GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static));
            ilGen.Emit(OpCodes.Ldc_I4, _numHook);
            ilGen.Emit(OpCodes.Callvirt, typeof(HookPool).GetMethod(nameof(HookPool.ReturnHook), [typeof(int)]));
            ilGen.Emit(OpCodes.Stloc, myHook);
            // List<object> listParameters = new();
            ilGen.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor([]));
            ilGen.Emit(OpCodes.Stloc, listParameters);
            // if (monHook.Actif) // Note : If false, goto label1
            ilGen.Emit(OpCodes.Ldloc_0);
            ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(Enabled)).GetGetMethod());
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ceq);
            ilGen.Emit(OpCodes.Stloc, ifEnabled);
            ilGen.Emit(OpCodes.Ldloc, ifEnabled);
            ilGen.Emit(OpCodes.Brtrue_S, label1);
            if (!IsStatic && pbInstance != null)
            {
                // listParameters.Add(myThis); // Note : myThis is the first parameter of this method
                ilGen.Emit(OpCodes.Ldloc, listParameters);
                ilGen.Emit(OpCodes.Ldarg, pbInstance.Position - 1);
                ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
            }
            if (MethodParameters != null && MethodParameters.Length > 0)
            {
                // listParameters.Add(<Param>); // Note : <Param> is/are the parameters of this method
                int numParam = (returnValue != null ? 2 : 1);
                foreach (ParameterInfo pi in MethodParameters)
                {
                    ilGen.Emit(OpCodes.Ldloc, listParameters);
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
                        ilGen.Emit(OpCodes.Ldloc, listParameters);
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
                        ilGen.Emit(OpCodes.Ldloc, listParameters);
                        ilGen.Emit(OpCodes.Ldnull);
                        ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.Add)));
                    }
                }
            }

            ilGen.MarkLabel(label1);
            // if (monHook.Actif) // Note : If false, goto label2
            ilGen.Emit(OpCodes.Ldloc, myHook);
            ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(Enabled)).GetGetMethod());
            ilGen.Emit(OpCodes.Ldc_I4_0);
            ilGen.Emit(OpCodes.Ceq);
            ilGen.Emit(OpCodes.Stloc, ifEnabled);
            ilGen.Emit(OpCodes.Ldloc, ifEnabled);
            ilGen.Emit(OpCodes.Brtrue_S, label2);
            if (_isDecorativeMethod)
            {
                if (_methodBefore != null)
                {
                    // monHook.MethodeAvant.Invoke(<Arg>, listParameters.ToArray()); // Note : Arg is null, if static, else contains the instance of the object (myThis, first parameter of this method)
                    ilGen.Emit(OpCodes.Ldloc, myHook);
                    ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(MethodBefore)).GetGetMethod());
                    if (!IsStatic)
                        ilGen.Emit(OpCodes.Ldarg, pbInstance.Position - 1);
                    else
                        ilGen.Emit(OpCodes.Ldnull);
                    ilGen.Emit(OpCodes.Ldloc, listParameters);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), [typeof(object), typeof(object[])]));
                    ilGen.Emit(OpCodes.Pop);
                }
                // monHook.AppelMethodeOriginale(<myThis>, listParameters.ToArray());  // Note : set <myThis> if it's not a static method, else <null>
                // ou monHook.AppelMethodeOriginale(<myThis>, object[]null);     // If the method has no parameter
                if (returnValue != null)
                    ilGen.Emit(OpCodes.Ldarg, returnValue.Position - 1);
                ilGen.Emit(OpCodes.Ldloc, myHook);
                if (!IsStatic)
                    ilGen.Emit(OpCodes.Ldarg, pbInstance.Position - 1);
                else
                    ilGen.Emit(OpCodes.Ldnull);
                if (MethodParameters.Length > 0)
                {
                    ilGen.Emit(OpCodes.Ldloc, listParameters);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                }
                else
                    ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetMethod(nameof(CallOriginalMethod), BindingFlags.Instance | BindingFlags.Public));
                // We keep the return of method, if the original method return a value (is not a void)
                if (returnValue != null)
                    ilGen.Emit(OpCodes.Stind_Ref);
                else
                    ilGen.Emit(OpCodes.Pop);
                if (_methodAfter != null)
                {
                    // monHook.MethodeApres.Invoke(<Arg>, listParameters.ToArray());
                    ilGen.Emit(OpCodes.Ldloc, myHook);
                    ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(MethodAfter)).GetGetMethod());
                    if (!IsStatic)
                        ilGen.Emit(OpCodes.Ldarg, pbInstance.Position - 1);
                    else
                        ilGen.Emit(OpCodes.Ldnull);
                    ilGen.Emit(OpCodes.Ldloc, listParameters);
                    ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                    ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), [typeof(object), typeof(object[])]));
                    ilGen.Emit(OpCodes.Pop);
                }
            }
            else
            {
                // monHook.ToMethode.Invoke(<Arg>, listParameters.ToArray()); // Note : Arg is null if static, else fill to myThis (first parameter of this method, the instance of the object)
                if (returnValue != null)
                    ilGen.Emit(OpCodes.Ldarg, returnValue.Position - 1);
                ilGen.Emit(OpCodes.Ldloc, myHook);
                ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetProperty(nameof(ToMethod)).GetGetMethod());
                if (!IsStatic)
                    ilGen.Emit(OpCodes.Ldarg, pbInstance.Position - 1);
                else
                    ilGen.Emit(OpCodes.Ldnull);
                ilGen.Emit(OpCodes.Ldloc, listParameters);
                ilGen.Emit(OpCodes.Callvirt, typeof(List<object>).GetMethod(nameof(List<object>.ToArray), Type.EmptyTypes));
                ilGen.Emit(OpCodes.Callvirt, typeof(MethodBase).GetMethod(nameof(MethodBase.Invoke), [typeof(object), typeof(object[])]));
                // We keep the return of method, if the original method return a value (is not a void)
                if (returnValue != null)
                    ilGen.Emit(OpCodes.Stind_Ref);
                else
                    ilGen.Emit(OpCodes.Pop);
            }
            // Note : goto label3 (end if, start of Else (label2))
            ilGen.Emit(OpCodes.Br_S, label3);
            ilGen.MarkLabel(label2);

            // InvokeParente_<NumHook>(params); // Note, we build the name of the method by concat with numHook (id of the hook). Note 2 : params is the list of parameters of the original methode (replaced) if there is/are
            if (!IsStatic)
                ilGen.Emit(OpCodes.Ldarg, pbInstance.Position - 1);
            if (MethodParameters.Length > 0)
            {
                int numParam = (returnValue != null ? 2 : 1);
                foreach (ParameterInfo pi in MethodParameters)
                {
                    ilGen.Emit(OpCodes.Ldarg, numParam++);
                }
            }
            ilGen.Emit(OpCodes.Callvirt, typeof(ManagedHook).GetMethod(nameof(CallOriginalMethod), BindingFlags.Instance | BindingFlags.Public));
            if (ReturnType != typeof(void))
                ilGen.Emit(OpCodes.Starg, returnValue.Position);

            // return <Resultat>; // Note : we return the result previously keep, if not a void
            ilGen.MarkLabel(label3);
            // End, we return false
            ilGen.Emit(OpCodes.Ldc_I4_0);
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
            corps.AppendLine($"using {nameof(HookManagerCore)}.{nameof(Modeles)};");
            corps.AppendLine("using System.Collections.Generic;");
            corps.AppendLine($"namespace {nameof(HookManagerCore)}.Hooks");
            corps.AppendLine("{");
            corps.AppendLine($"public class {HookPool.CLASS_NAME}{_numHook}");
            corps.AppendLine("{");

            // Méthode passerelle
            if ((!Debugger.IsAttached) || (!HookPool.GetInstance().ModeInternalDebug))
                corps.AppendLine("[System.Diagnostics.DebuggerNonUserCode()]");
            corps.Append("private static bool ");
            corps.Append($"{HookPool.METHOD_NAME}{_numHook}({(ReturnType == typeof(void) ? "" : "ref object __result")}{(IsStatic ? "" : $"{(ReturnType == typeof(void) ? "" : ", ")}object __instance = null")}");
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
            corps.AppendLine($"if (myHook.{nameof(Enabled)})");
            corps.AppendLine("{");
            if (!IsStatic)
                corps.AppendLine("param.Add(__instance);");
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

            corps.AppendLine($"if (myHook.{nameof(Enabled)})");
            corps.AppendLine("{");
            if (_isDecorativeMethod)
            {
                if (_methodBefore != null)
                    corps.AppendLine($"myHook.{nameof(MethodBefore)}.Invoke({(IsStatic ? "null" : "__instance")}, param.ToArray());");
                if (ReturnType != typeof(void))
                    corps.Append("object retour = ");
                corps.AppendLine($"myHook.{nameof(CallOriginalMethod)}({(IsStatic ? "null" : "__instance")}, {(MethodParameters.Length > 0 ? ", param.ToArray()" : "null")});");
                if (_methodAfter != null)
                {
                    corps.AppendLine($"myHook.{nameof(MethodAfter)}.Invoke({(IsStatic ? "null" : "__instance")}, param.ToArray());");
                }
                if (ReturnType != typeof(void))
                    corps.AppendLine("return retour;");
            }
            else
            {
                if (ReturnType != typeof(void))
                    corps.Append("__result = ");
                corps.AppendLine($"myHook.{nameof(ToMethod)}.Invoke({(IsStatic ? "null" : "__instance")}, param.ToArray());");
            }
            corps.AppendLine("}");
            corps.AppendLine("else");
            corps.AppendLine("{");
            if (ReturnType != typeof(void))
                corps.Append("__result = ");
            corps.Append($"myHook.{nameof(CallOriginalMethod)}");
            corps.Append($"({(IsStatic && !IsConstructor ? "" : "__instance")}");
            if (MethodParameters.Length > 0)
                for (int i = 0; i < MethodParameters.Length; i++)
                {
                    if ((i > 0) || (!IsStatic || IsConstructor))
                        corps.Append(", ");
                    corps.Append($"param{(i + 1)}");
                }
            corps.AppendLine(");");
            corps.AppendLine("}");
            corps.AppendLine("return false;");
            corps.AppendLine("}");

            corps.AppendLine("}");
            corps.AppendLine("}");

            CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary, warningLevel: 0, platform: Platform.AnyCpu, optimizationLevel: (HookPool.GetInstance().ModeInternalDebug ? OptimizationLevel.Debug : OptimizationLevel.Release), checkOverflow: HookPool.GetInstance().ModeInternalDebug);

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(corps.ToString(), new CSharpParseOptions(LanguageVersion.CSharp12, DocumentationMode.None));
            List<MetadataReference> references =
            [
                MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().First(asm => asm.GetName() != null && asm.GetName().Name == "System.Runtime").Location),
            ];
            string filename = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + ".dll";
            CSharpCompilation result = CSharpCompilation.Create(filename, [syntaxTree], references, options);
            MemoryStream ms = new();
            Microsoft.CodeAnalysis.Emit.EmitResult retour = result.Emit(ms);

            if (retour.Success)
            {
                if (HookPool.GetInstance().ModeInternalDebug)
                {
                    File.WriteAllBytes(Path.Combine(Path.GetTempPath(), filename), ms.ToArray());
                    File.WriteAllText(Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(filename) + ".cs"), corps.ToString());
                }
                Assembly compiledAssembly = Assembly.Load(ms.ToArray());
                ms.Dispose();
                if (compiledAssembly != null)
                    _myDynamicClass = compiledAssembly.CreateInstance($"{nameof(HookManagerCore)}.Hooks.{HookPool.CLASS_NAME}{_numHook}");
                else
                    throw new CommonHookManagerException("Unable to instanciate script");
            }
            else
                throw new CompilationException(retour.Diagnostics);
        }

        #endregion
    }
}
