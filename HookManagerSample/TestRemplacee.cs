using System;
using System.Runtime.CompilerServices;

namespace HookManagerSample
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
