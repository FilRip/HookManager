using System.Reflection;

using HookManagerCore;
using HookManagerCore.Modeles;

namespace TestHookManagerCore
{
    internal class Program
    {
#pragma warning disable IDE0060 // Supprimer le paramètre inutilisé
        static void Main(string[] args)
#pragma warning restore IDE0060 // Supprimer le paramètre inutilisé
        {
            // Cette ligne active le mode debug pas à pas dans les fichiers générés pendant l'exécution contenant les "fausses" méthodes gérant le remplacement.
            // Ralenti le système, à n'utiliser que pour débogger la librairie HookManager (vous pouvez tjr, même sans ca, débogger du code remplacé, ca n'a pas de rapport)
            // Utilise le répertoire TEMP (définit par la variable d'environement) pour y stocker les fichiers générés
            HookPool.GetInstance().ModeInternalDebug = true;

            Teste monTest = new();
            monTest.EcrireConsole();

            System.Diagnostics.Stopwatch sw = new();
            sw.Start();
            // Remplace automatiquement toutes les méthodes avec attribut HookManager
            HookPool.GetInstance().InitializeAllAttributeHook();
            sw.Stop();
            Console.WriteLine("Time generate : " + sw.ElapsedMilliseconds.ToString());
            /*Console.WriteLine("Liste des assembly :");
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine(ass.GetName()?.Name);*/
            Console.WriteLine("");

            monTest.EcrireConsole();
            Console.WriteLine("");
            monTest.EcrireConsole2();
            Console.WriteLine("");
            monTest.EcrireConsoleEnDelegue();
            Console.WriteLine("");
            monTest.EcrireConsoleParam("ok", "it works");
            Console.WriteLine();
            Teste.TestStatic();
            Console.WriteLine("");
            monTest.TestAvecParams("TREILLE", "Philippe");
            Console.WriteLine("");
            monTest.TesteVirtual();
            Console.WriteLine("");
            Console.WriteLine(monTest.TesteRetour());
            Console.WriteLine("");
            Console.WriteLine("Test disable hook");
            HookPool.GetInstance().ReturnHook(typeof(Teste).GetMethod(nameof(Teste.EcrireConsole), BindingFlags.Instance | BindingFlags.Public)).Disable();
            monTest.EcrireConsole();

            Console.WriteLine("Test hook api windows (y/n) ?");
            if (Console.ReadKey(false).KeyChar.ToString().ToLower() == "y")
            {
                NativeHookDemo ndemo = new();
                ndemo.Main();
            }
            Console.WriteLine("");

            GacHook hookConsole = HookPool.GetInstance().AddGacHook(typeof(Console).GetMethod(nameof(Console.WriteLine), BindingFlags.Static | BindingFlags.Public, null, [typeof(string)], null), typeof(TesteHook).GetMethod(nameof(TesteHook.HookSystemConsole)));
            Console.WriteLine("test call methode in the GAC : System.Console");
            hookConsole.Remove();

            Classe1 classe1 = new();
            Classe2 classe2 = new();
            Classe3 classe3 = new();

            HookPool.GetInstance().ReplaceInterfaceMethods(typeof(IInterface1), typeof(ClasseRemplaceInterface));
            classe1.TestMoi();
            classe2.TestMoi2();
            classe3.TestMoi();
            Console.WriteLine("");

            TestRemplacee tr = new();
            tr.AncienneMethode();
            _ = HookPool.GetInstance().AddReplacement(typeof(TestRemplacee).GetMethod(nameof(TestRemplacee.AncienneMethode), BindingFlags.Public | BindingFlags.Instance), typeof(TestRemplacement).GetMethod(nameof(TestRemplacement.NouvelleMethode)));
            tr.AncienneMethode();
            Console.WriteLine("");

            Console.WriteLine("Change property");
            Console.WriteLine("Property before : " + monTest.Valeur);
            HookPool.GetInstance().AddHook(typeof(Teste).GetProperty(nameof(Teste.Valeur), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), typeof(TesteHook).GetMethod(nameof(TesteHook.GetValeur), BindingFlags.Public | BindingFlags.Static));
            Console.WriteLine("Property after : " + monTest.Valeur);
            Console.WriteLine("");

            Console.WriteLine("Test decorate method");
            monTest.TestDeco();
            Console.WriteLine("");

            ConstructorInfo ci = typeof(Teste).GetConstructors()[0];

            Console.WriteLine("Test decorate constructor");
            MethodInfo miAvant = typeof(TesteHook).GetMethod(nameof(TesteHook.AvantConstructeur), BindingFlags.Static | BindingFlags.Public);
            MethodInfo miApres = typeof(TesteHook).GetMethod(nameof(TesteHook.ApresConstructeur), BindingFlags.Static | BindingFlags.Public);
            ManagedHook mh = HookPool.GetInstance().AddConstructorDecoration(ci, miAvant, miApres);
            _ = new Teste();
            Console.WriteLine("");
            mh.Disable();

            Console.WriteLine("Test replacement constructor");
            MethodInfo miNewConstructeur = typeof(TesteHook).GetMethod(nameof(TesteHook.RemplaceConstructeur), BindingFlags.Static | BindingFlags.Public);
            mh = HookPool.GetInstance().AddHook(ci, miNewConstructeur);

            _ = new Teste();
            Console.WriteLine("");

            Console.WriteLine("Disable replacement constructor");
            mh.Disable();
            monTest = new();
            Console.WriteLine("");

            Console.WriteLine("Test Events");
            HookPool.GetInstance().AddHook(typeof(Teste).GetEvent(nameof(Teste.MonEvent)), typeof(TesteHook).GetMethod(nameof(TesteHook.IntercepteAjoutEvent), BindingFlags.Static | BindingFlags.Public), typeof(TesteHook).GetMethod(nameof(TesteHook.IntercepteSupprimeEvent), BindingFlags.Static | BindingFlags.Public));
            monTest.MonEvent += MonTest_MonEvent;
            monTest.DeclencheEvent();
            monTest.MonEvent -= MonTest_MonEvent;
            monTest.DeclencheEvent();
            Console.WriteLine("");

            /*Console.WriteLine("Liste des assembly :");
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine(ass.GetName()?.Name);*/

            Console.WriteLine(Environment.NewLine + "Press any key to leave...");
            Console.ReadLine();
        }

        private static void MonTest_MonEvent(object sender, EventArgs e)
        {
            Console.WriteLine("Execution MyEvent");
        }
    }
}
