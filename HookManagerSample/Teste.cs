using System;

using HookManager.Attributes;

namespace HookManagerSample
{
    public class Teste
    {
        public Teste()
        {
            try
            {
                Console.WriteLine($"My constructor {this}");
                switch (_valeur)
                {
                    case "Instance1":
                        Console.WriteLine("Instance1");
                        break;
                    default:
                        Console.WriteLine("Instance2");
                        break;
                }
//#pragma warning disable S112 // General or reserved exceptions should never be thrown
                //throw new Exception("Fausse erreur"); // Uncomment to test exception during constructor
//#pragma warning restore S112 // General or reserved exceptions should never be thrown
                // C'est normal ce warning, c'est pour tester la copie de méthode
//#pragma warning disable CS0162
                Console.WriteLine("Code executed");
//#pragma warning restore CS0162
            }
            catch (Exception)
            {
                Console.WriteLine("--- Intercept Exception");
            }
            finally
            {
                AppelDepuisConstructeur();
            }
            Console.WriteLine("End try/Catch");
        }

        private void AppelDepuisConstructeur()
        {
            Console.WriteLine("Finally called");
        }

        private string _valeur = "Instance1";

        //[HookPropriete(Classe = typeof(TesteHook), GetMethode = nameof(TesteHook.GetValeur))]
#pragma warning disable S2292 // Trivial properties should be auto-implemented
        public string Valeur
        {
            get { return _valeur; }
            set { _valeur = value; }
        }
#pragma warning restore S2292 // Trivial properties should be auto-implemented

        [HookMethod(Classe = typeof(TesteHook), MethodName = nameof(TesteHook.HookEcrireConsole))]
        public void EcrireConsole()
        {
            Console.WriteLine("Instance Hello World!");
        }

        [HookMethod(Classe = typeof(TesteHook), MethodName = nameof(TesteHook.HookEcrireConsole))]
        public void EcrireConsole2()
        {
            Console.WriteLine("Instance hello world same method of replacement");
        }

        [HookMethod(Classe = typeof(TesteHook), MethodName = nameof(TesteHook.EcrireConsoleAvecParam))]
        // Warning désactivé. C'est normal, c'est pour les tests
#pragma warning disable IDE0060, IDE0079
        public void EcrireConsoleParam(string param1, string param2)
#pragma warning restore IDE0060, IDE0079
        {
            Console.WriteLine("Instance with parameter : " + param1);
        }

        public delegate void delegateEcrireConsole();
        public void EcrireConsoleEnDelegue()
        {
            delegateEcrireConsole monDelegue = new(EcrireConsole);
            Console.WriteLine("Call delegate");
            monDelegue.Invoke();
        }

        [HookMethod(Classe = typeof(TesteHook), MethodName = nameof(TesteHook.HookStatic))]
        public static void TestStatic()
        {
            Console.WriteLine("Static");
        }

        [HookMethod(Classe = typeof(TesteHook), MethodName = nameof(TesteHook.TestAvecParamsHooked))]
        public void TestAvecParams(string param1, string param2)
        {
            Console.WriteLine($"param1={param1}, param2={param2}");
        }

        [HookMethod(Classe = typeof(TesteHook), MethodName = nameof(TesteHook.HookVirtual))]
        public virtual void TesteVirtual()
        {
            Console.WriteLine("testVirtual");
        }

#pragma warning disable S3400 // Methods should not return constants
        [HookMethod(Classe = typeof(TesteHook), MethodName = nameof(TesteHook.HookRetour))]
        public string TesteRetour()
        {
            return "testReturn";
        }
#pragma warning restore S3400 // Methods should not return constants

        [DecorateMethod(
            MethodNameBefore = nameof(HookManagerSample) + "." + nameof(TesteHook) + "." + nameof(TesteHook.ExecuteAvant),
            MethodNameAfter = "HookManagerSample.TesteHook.ExecuteApres")]
        public void TestDeco()
        {
            Console.WriteLine("Body of method");
        }

        public delegate void DelegateMonEvent(object sender, EventArgs e);
        public event DelegateMonEvent MonEvent;

        public void DeclencheEvent()
        {
            MonEvent?.Invoke(this, new EventArgs());
        }
    }
}
