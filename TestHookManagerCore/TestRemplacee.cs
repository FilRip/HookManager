using System;
using System.Runtime.CompilerServices;

namespace TestHookManagerCore
{
    public class TestRemplacee
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void AncienneMethode()
        {
            Console.WriteLine("Ancienne méthode");
        }
    }
}
