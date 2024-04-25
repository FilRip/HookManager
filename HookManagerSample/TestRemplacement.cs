using System;
using System.Runtime.CompilerServices;

namespace HookManagerSample
{
#pragma warning disable S1118 // Utility classes should not have public constructors
    public class TestRemplacement
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void NouvelleMethode(object monThis)
        {
            Console.WriteLine("Nouvelle méthode, classe de base : " + monThis.ToString());
        }
    }
#pragma warning restore S1118 // Utility classes should not have public constructors
}
