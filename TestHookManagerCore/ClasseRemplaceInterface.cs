using System;

using HookManagerCore.Modeles;

namespace TestHookManagerCore
{
#pragma warning disable S1118 // Utility classes should not have public constructors
    public class ClasseRemplaceInterface
    {
        // Warning disabled, it's for test
#pragma warning disable IDE0060
        public static void TestMoi(object instance, ManagedHook mg)
#pragma warning restore IDE0060
        {
            Console.WriteLine("Hooked Interface implementation");
        }
    }
#pragma warning restore S1118 // Utility classes should not have public constructors
}
