using System;
using System.Reflection;

using HookManagerCore;
using HookManagerCore.Modeles;

namespace TestHookManagerCore
{
    // Warning désactivé. C'est normal, c'est pour les tests
#pragma warning disable IDE0060, IDE0079
    public static class TesteHook
    {
        public static object RemplaceConstructeur(object instance)
        {
            Console.WriteLine($"Constructor replace, {instance}");
            // On peut appeler l'ancien constructeur avec ces 2 lignes :
            Console.WriteLine("Call older constructor (pour test) :");
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            mh.CallOriginalMethod(instance);
            return instance;
        }

        public static void AvantConstructeur(object instance)
        {
            Console.WriteLine($"Hooked Before Constructor, {instance}");
        }

        public static void ApresConstructeur(object instance)
        {
            Console.WriteLine($"Hooked After Constructor, {instance}");
        }

        public static string GetValeur(object instance)
        {
            return "Instance2";
        }

        public static void EcrireConsoleAvecParam(object instance, string param1, string param2, string param3)
        {
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            Console.Write("Hooked ");
            mh?.CallOriginalMethod(instance, param1, param2);
        }

        public static void HookEcrireConsole(object instance)
        {
            ManagedHook monHook = HookPool.GetInstance().ReturnHook();
            Console.Write("Hooked ");
            monHook.CallOriginalMethod(instance);
        }

        public static void HookStatic()
        {
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            Console.Write("Hooked ");
            mh.CallOriginalMethod();
        }

        public static void TestAvecParamsHooked(object instance, string param1, string param2, string param3)
        {
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            Console.Write("Hooked ");
            mh.CallOriginalMethod(instance, param1, param2);
        }

        public static void HookVirtual(object instance)
        {
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            Console.Write("Hooked ");
            mh.CallOriginalMethod(instance);
        }

        public static string HookRetour(object instance)
        {
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            return "Hooked " + mh.CallOriginalMethod(instance);
        }

        public static void HookSystemConsole(string param1)
        {
            GacHook hook = HookPool.GetInstance().ReturnGacHook(typeof(Console).GetMethod(nameof(Console.WriteLine), BindingFlags.Public | BindingFlags.Static, null, [typeof(string)], null));
            hook?.Call<object>(null, "Hooked " + param1);
        }

        public static void ExecuteAvant(object instance)
        {
            Console.WriteLine("Before body");
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            mh.CallOriginalMethod(instance, null);
        }

        public static void ExecuteApres(object instance)
        {
            Console.WriteLine("After body");
        }

        public static void IntercepteAjoutEvent(object instance, Delegate leDelegue)
        {
            Console.WriteLine("Detect add subscriber to event : " + leDelegue.ToString().Replace("+", "."));
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            mh.CallOriginalMethod(instance, leDelegue);
        }

        public static void IntercepteSupprimeEvent(object instance, Delegate leDelegue)
        {
            Console.WriteLine("Detect remove subscriber to event : " + leDelegue.ToString().Replace("+", "."));
            ManagedHook mh = HookPool.GetInstance().ReturnHook();
            mh.CallOriginalMethod(instance, leDelegue);
        }
    }
#pragma warning restore IDE0060, IDE0079
}
