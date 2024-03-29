﻿using System;

using HookManager.Attributes;

namespace HookManagerSample
{
    public class Teste
    {
        public Teste()
        {
            try
            {
                Console.WriteLine($"Mon constructeur {this}");
                switch (_valeur)
                {
                    case "Instance1":
                        Console.WriteLine("Instance1");
                        break;
                    default:
                        Console.WriteLine("Instance2");
                        break;
                }
                throw new Exception("Fausse erreur");
// C'est normal ce warning, c'est pour tester la copie de méthode
#pragma warning disable CS0162
                Console.WriteLine("Code a ne pas executer");
#pragma warning restore CS0162
            }
            catch (Exception)
            {
                Console.WriteLine("--- Exception catchée");
            }
            finally
            {
                AppelDepuisConstructeur();
            }
            Console.WriteLine("Fin try/Catch");
        }

        private void AppelDepuisConstructeur()
        {
            Console.WriteLine("Appel méthode depuis constructeur");
        }

        private string _valeur = "Instance1";

        //[HookPropriete(Classe = typeof(TesteHook), GetMethode = nameof(TesteHook.GetValeur))]
        public string Valeur
        {
            get { return _valeur; }
            set { _valeur = value; }
        }

        [HookMethode(Classe = typeof(TesteHook), NomMethode = nameof(TesteHook.HookEcrireConsole))]
        public void EcrireConsole()
        {
            Console.WriteLine("Instance Hello World!");
        }

        [HookMethode(Classe = typeof(TesteHook), NomMethode = nameof(TesteHook.HookEcrireConsole))]
        public void EcrireConsole2()
        {
            Console.WriteLine("Instance hello world même méthode de substitution");
        }

        [HookMethode(Classe = typeof(TesteHook), NomMethode = nameof(TesteHook.EcrireConsoleAvecParam))]
        // Warning désactivé. C'est normal, c'est pour les tests
#pragma warning disable IDE0060, IDE0079
        public void EcrireConsoleParam(string param1, string param2)
#pragma warning restore IDE0060, IDE0079
        {
            Console.WriteLine("Instance avec parametre : " + param1);
        }

        public delegate void delegateEcrireConsole();
        public void EcrireConsoleEnDelegue()
        {
            delegateEcrireConsole monDelegue = new(EcrireConsole);
            Console.WriteLine("Call delegate");
            monDelegue.Invoke();
        }

        [HookMethode(Classe = typeof(TesteHook), NomMethode = nameof(TesteHook.HookStatic))]
        public static void TestStatic()
        {
            Console.WriteLine("Static");
        }

        [HookMethode(Classe = typeof(TesteHook), NomMethode = nameof(TesteHook.TestAvecParamsHooked))]
        public void TestAvecParams(string param1, string param2)
        {
            Console.WriteLine($"param1={param1}, param2={param2}");
        }

        [HookMethode(Classe = typeof(TesteHook), NomMethode = nameof(TesteHook.HookVirtual))]
        public virtual void TesteVirtual()
        {
            Console.WriteLine("testeVirtual");
        }

        [HookMethode(Classe = typeof(TesteHook), NomMethode = nameof(TesteHook.HookRetour))]
        public string TesteRetour()
        {
            return "testRetour";
        }

        [DecorationMethode(
            NomMethodeAvant = nameof(HookManagerSample) + "." + nameof(TesteHook) + "." + nameof(TesteHook.ExecuteAvant),
            NomMethodeApres = "HookManagerSample.TesteHook.ExecuteApres")]
        public void TestDeco()
        {
            Console.WriteLine("Corps de la méthode");
        }

        public delegate void DelegateMonEvent(object sender, EventArgs e);
        public event DelegateMonEvent MonEvent;

        public void DeclencheEvent()
        {
            MonEvent?.Invoke(this, new EventArgs());
        }
    }
}
