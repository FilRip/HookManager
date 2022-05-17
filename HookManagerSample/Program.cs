using System;
using System.Reflection;

using HookManager;
using HookManager.Modeles;

namespace HookManagerSample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Cette ligne active le mode debug pas à pas dans les fichiers générés pendant l'exécution contenant les "fausses" méthodes gérant le remplacement.
            // Ralenti le système, à n'utiliser que pour débogger la librairie HookManager (vous pouvez tjr, même sans ca, débogger du code remplacé, ca n'a pas de rapport)
            // Utilise le répertoire TEMP (définit par la variable d'environement) pour y stocker les fichiers générés
            HookPool.GetInstance().ModeDebugInterne = true;

            Teste monTest = new();
            monTest.EcrireConsole();

            System.Diagnostics.Stopwatch sw = new();
            sw.Start();
            // Remplace automatiquement toutes les méthodes avec attribut HookManager
            HookPool.GetInstance().PrepareMethodesTaggees();
            sw.Stop();
            Console.WriteLine("Durée génération : " + sw.ElapsedMilliseconds.ToString());
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
            monTest.EcrireConsoleParam("ok", "ca fonctionne");
            Console.WriteLine();
            Teste.TestStatic();
            Console.WriteLine("");
            monTest.TestAvecParams("TREILLE", "Philippe");
            Console.WriteLine("");
            monTest.TesteVirtual();
            Console.WriteLine("");
            Console.WriteLine(monTest.TesteRetour());
            Console.WriteLine("");

            Console.WriteLine("Test désactive");
            HookPool.GetInstance().RetourneHook(typeof(Teste).GetMethod(nameof(Teste.EcrireConsole), BindingFlags.Instance | BindingFlags.Public)).Desactive();
            monTest.EcrireConsole();
            
            Console.WriteLine("Test hook api windows (o/n) ?");
            if (Console.ReadKey(false).KeyChar.ToString().ToLower() == "o")
            {
                NativeHookDemo ndemo = new();
                ndemo.Main();
            }
            Console.WriteLine("");

            GACHook hookConsole = HookPool.GetInstance().AjouterGACHook(typeof(Console).GetMethod(nameof(Console.WriteLine), BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null), typeof(TesteHook).GetMethod(nameof(TesteHook.HookSystemConsole)));
            Console.WriteLine("test appel méthode dans le GAC : System.Console");
            hookConsole.Remove();

            Classe1 classe1 = new();
            Classe2 classe2 = new();
            Classe3 classe3 = new();

            HookPool.GetInstance().RemplaceMethodesInterface(typeof(IInterface1), typeof(ClasseRemplaceInterface));
            classe1.TestMoi();
            classe2.TestMoi2();
            classe3.TestMoi();
            Console.WriteLine("");

            TestRemplacee tr = new();
            tr.AncienneMethode();
            MethodeRemplacementHook mrh = HookPool.GetInstance().AjouterRemplacement(typeof(TestRemplacee).GetMethod(nameof(TestRemplacee.AncienneMethode), BindingFlags.Public | BindingFlags.Instance), typeof(TestRemplacement).GetMethod(nameof(TestRemplacement.NouvelleMethode)));
            tr.AncienneMethode();
            Console.WriteLine("");

            Console.WriteLine("Change propriété");
            //Console.WriteLine("Propriété avant : " + monTest.Valeur);
            //HookPool.GetInstance().AjouterHook(typeof(Teste).GetProperty(nameof(Teste.Valeur), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), typeof(TesteHook).GetMethod(nameof(TesteHook.GetValeur), BindingFlags.Public | BindingFlags.Static));
            Console.WriteLine("Propriété après : " + monTest.Valeur);
            Console.WriteLine("");

            Console.WriteLine("Test décoration de méthode");
            monTest.TestDeco();
            Console.WriteLine("");

            Console.WriteLine(Environment.NewLine + "Appuyez sur <Entrer> pour quitter...");
            Console.ReadLine();
        }
    }
}
