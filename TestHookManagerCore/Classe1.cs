using System;

namespace TestHookManagerCore
{
    class Classe1 : IInterface1
    {
        public void TestMoi()
        {
            Console.WriteLine("Method originale implemented from interface");
        }
    }
}
