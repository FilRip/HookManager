using System;

using HookManager.Modeles;

namespace HookManagerSample
{
    public class ClasseRemplaceInterface
    {
        // Warning désactivé. C'est normal, c'est pour les tests, cette nouvelle méthode, remplaçant l'ancienne, n'utilise pas forcément tous les paramètres
#pragma warning disable IDE0060
        public static void TestMoi(object instance, ManagedHook mg)
#pragma warning restore IDE0060
        {
            Console.WriteLine("Hooked Interface implementation");
        }
    }
}
