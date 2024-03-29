﻿using System;
using System.Reflection;

using HookManager;
using HookManager.Modeles;

namespace HookManagerSample
{
    // Warning désactivé. C'est normal, c'est pour les tests
#pragma warning disable IDE0060, IDE0079
    public class TesteHook
    {
        public static object RemplaceConstructeur(object instance)
        {
            Console.WriteLine($"Constructeur remplacé, {instance}");
            // On peut appeler l'ancien constructeur avec ces 2 lignes :
            Console.WriteLine("Appel ancien constructeur (pour test) :");
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            mh.AppelMethodeOriginale(instance);
            return instance;
        }

        public static void AvantConstructeur(object instance)
        {
            Console.WriteLine($"Hooked Avant Constructeur, {instance}");
        }

        public static void ApresConstructeur(object instance)
        {
            Console.WriteLine($"Hooked Apres Constructeur, {instance}");
        }

        public static string GetValeur(object instance)
        {
            return "Instance2";
        }

        public static void EcrireConsoleAvecParam(object instance, string param1, string param2, string param3)
        {
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            Console.Write("Hooked ");
            if (mh != null)
                mh.AppelMethodeOriginale(instance, param1, param2);
        }

        public static void HookEcrireConsole(object instance)
        {
            ManagedHook monHook = HookPool.GetInstance().RetourneHook();
            Console.Write("Hooked ");
            monHook.AppelMethodeOriginale(instance);
        }

        public static void HookStatic()
        {
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            Console.Write("Hooked ");
            mh.AppelMethodeOriginale();
        }

        public static void TestAvecParamsHooked(object instance, string param1, string param2, string param3)
        {
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            Console.Write("Hooked ");
            mh.AppelMethodeOriginale(instance, param1, param2);
        }

        public static void HookVirtual(object instance)
        {
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            Console.Write("Hooked ");
            mh.AppelMethodeOriginale(instance);
        }

        public static string HookRetour(object instance)
        {
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            return "Hooked " + mh.AppelMethodeOriginale(instance);
        }

        public static void HookSystemConsole(string param1)
        {
            GACHook hook = HookPool.GetInstance().RetourneGACHook(typeof(Console).GetMethod(nameof(Console.WriteLine), BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) }, null));
            if (hook != null)
                hook.Call<object>(null, "Hooked " + param1);
        }

        public static void ExecuteAvant(object instance)
        {
            Console.WriteLine("Avant corps");
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            mh.AppelMethodeOriginale(instance, null);
        }

        public static void ExecuteApres(object instance)
        {
            Console.WriteLine("Apres corps");
        }

        public static void IntercepteAjoutEvent(object instance, Delegate leDelegue)
        {
            Console.WriteLine("Détecte ajout d'un abonné à l'event : " + leDelegue.ToString().Replace("+","."));
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            mh.AppelMethodeOriginale(instance, new object[] { leDelegue });
        }

        public static void IntercepteSupprimeEvent(object instance, Delegate leDelegue)
        {
            Console.WriteLine("Détecte suppression d'un abonné à l'event : " + leDelegue.ToString().Replace("+", "."));
            ManagedHook mh = HookPool.GetInstance().RetourneHook();
            mh.AppelMethodeOriginale(instance, new object[] { leDelegue });
        }
    }
#pragma warning restore IDE0060, IDE0079
}
