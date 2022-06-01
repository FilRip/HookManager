using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookManager.Helpers
{
    internal static class ExtensionsByteArray
    {
        internal static Int32 ReadInt32(this byte[] listeOctets, ref int offset)
        {
            Int32 retour = BitConverter.ToInt32(listeOctets, offset);
            offset += 4;
            return retour;
        }

        internal static Int64 ReadInt64(this byte[] listeOctets, ref int offset)
        {
            Int64 retour = BitConverter.ToInt64(listeOctets, offset);
            offset += 8;
            return retour;
        }

        internal static double ReadDouble(this byte[] listeOctets, ref int offset)
        {
            double retour = BitConverter.ToDouble(listeOctets, offset);
            offset += 8;
            return retour;
        }

        internal static Int16 ReadInt16(this byte[] listeOctets, ref int offset)
        {
            Int16 retour = BitConverter.ToInt16(listeOctets, offset);
            offset += 2;
            return retour;
        }

        internal static Single ReadSingle(this byte[] listeOctets, ref int offset)
        {
            Single retour = BitConverter.ToSingle(listeOctets, offset);
            offset += 4;
            return retour;
        }
    }
}
