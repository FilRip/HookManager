using System;

namespace HookManager.Helpers
{
    internal static class ExtensionsByteArray
    {
        internal static sbyte ReadSByte(this byte[] listeOctets, ref int offset)
        {
            sbyte retour = Convert.ToSByte(listeOctets[offset]);
            offset++;
            return retour;
        }

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

        internal static uint ReadUInt32(this byte[] listeOctets, ref int offset)
        {
            uint retour = BitConverter.ToUInt32(listeOctets, offset);
            offset += 4;
            return retour;
        }

        internal static UInt16 ReadUInt16(this byte[] listeOctets, ref int offset)
        {
            UInt16 retour = BitConverter.ToUInt16(listeOctets, offset);
            offset += 2;
            return retour;
        }

        internal static UInt64 ReadUInt64(this byte[] listeOctets, ref int offset)
        {
            UInt64 retour = BitConverter.ToUInt64(listeOctets, offset);
            offset += 8;
            return retour;
        }

        internal static uint ReadCompressedUInt32(this byte[] listeOctets, ref int offset)
        {
            byte premierOctet = listeOctets[offset++];
            if ((premierOctet & 0x80) == 0)
                return premierOctet;
            if ((premierOctet & 0x40) == 0)
                return ((uint)(premierOctet & ~0x80) << 8) | listeOctets[offset++];
            return ((uint)(premierOctet & ~0xC0) << 24) | (uint)listeOctets[offset++] << 16 | (uint)listeOctets[offset++] << 8 | listeOctets[offset++];
        }

        internal static int ReadCompressedInt32(this byte[] listeOctets, ref int offset)
        {
            byte premierOctet = listeOctets[offset++];
            offset = 0;
            int u = (int)listeOctets.ReadCompressedUInt32(ref offset);
            int v = u >> 1;
            if ((u & 1) == 0)
                return u;
            return (premierOctet & 0xC0) switch
            {
                0 or 0x40 => v - 0x40,
                0x80 => v - 0x2000,
                _ => v - 0x10000000,
            };
        }
    }
}
