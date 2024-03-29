﻿using System;
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
            HookPool.GetInstance().InitialiseTousHookParAttribut();
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
            ((ManagedHook)(HookPool.GetInstance().RetourneHook(typeof(Teste).GetMethod(nameof(Teste.EcrireConsole), BindingFlags.Instance | BindingFlags.Public)))).Desactive();
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
            Console.WriteLine("Propriété avant : " + monTest.Valeur);
            HookPool.GetInstance().AjouterHook(typeof(Teste).GetProperty(nameof(Teste.Valeur), BindingFlags.Public | BindingFlags.Instance).GetGetMethod(), typeof(TesteHook).GetMethod(nameof(TesteHook.GetValeur), BindingFlags.Public | BindingFlags.Static));
            Console.WriteLine("Propriété après : " + monTest.Valeur);
            Console.WriteLine("");

            Console.WriteLine("Test décoration de méthode");
            monTest.TestDeco();
            Console.WriteLine("");

            ConstructorInfo ci = typeof(Teste).GetConstructors()[0];

            Console.WriteLine("Test décoration constructeur");
            MethodInfo miAvant = typeof(TesteHook).GetMethod(nameof(TesteHook.AvantConstructeur), BindingFlags.Static | BindingFlags.Public);
            MethodInfo miApres = typeof(TesteHook).GetMethod(nameof(TesteHook.ApresConstructeur), BindingFlags.Static | BindingFlags.Public);
            ManagedHook mh = HookPool.GetInstance().AjouterDecorationConstructeur(ci, miAvant, miApres);
            monTest = new();
            Console.WriteLine("");
            mh.Desactive();

            Console.WriteLine("Test remplacement constructeur");
            MethodInfo miNewConstructeur = typeof(TesteHook).GetMethod(nameof(TesteHook.RemplaceConstructeur), BindingFlags.Static | BindingFlags.Public);
            mh = HookPool.GetInstance().AjouterHook(ci, miNewConstructeur);

            monTest = new();
            Console.WriteLine("");

            Console.WriteLine("Désactive remplacement constructeur");
            mh.Desactive();
            monTest = new();
            Console.WriteLine("");

            Console.WriteLine("Test Events");
            HookPool.GetInstance().AjouterHook(typeof(Teste).GetEvent(nameof(Teste.MonEvent)), typeof(TesteHook).GetMethod(nameof(TesteHook.IntercepteAjoutEvent), BindingFlags.Static | BindingFlags.Public), typeof(TesteHook).GetMethod(nameof(TesteHook.IntercepteSupprimeEvent), BindingFlags.Static | BindingFlags.Public));
            monTest.MonEvent += MonTest_MonEvent;
            monTest.DeclencheEvent();
            monTest.MonEvent -= MonTest_MonEvent;
            monTest.DeclencheEvent();
            Console.WriteLine("");

            /*Console.WriteLine("Liste des assembly :");
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
                Console.WriteLine(ass.GetName()?.Name);*/

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine(Environment.NewLine + "Appuyez sur <Entrer> pour quitter...");
                Console.ReadLine();
            }
        }

        private static void MonTest_MonEvent(object sender, EventArgs e)
        {
            Console.WriteLine("Execution MonEvent");
        }
    }
}
