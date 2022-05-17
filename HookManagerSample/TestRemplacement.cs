using System;
using System.Runtime.CompilerServices;

namespace HookManagerSample
{
    public class TestRemplacement
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void NouvelleMethode(object monThis)
        {
            Console.WriteLine("Nouvelle méthode, classe de base : " + monThis.ToString());
        }
    }
}
