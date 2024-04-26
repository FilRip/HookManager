using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace HookManagerCore.Helpers
{
    /// <summary>
    /// Classe contenant des méthodes d'extensions pour les chaines de caractères<br/>
    /// En particulier les chaines de caractères non managées
    /// </summary>
    public static class ExtensionsString
    {
        /// <summary>
        /// Lire une chaine de caractères non managée en ASCII
        /// </summary>
        /// <param name="ptr">Pointeur vers la chaine de caractères</param>
        public static string ReadASCIINullTerminatedString(this IntPtr ptr)
        {
            List<byte> strBytes = [];
            int count = 0;
            byte cur;

            // Terminated by 2ishx \0
            while (true)
            {
                cur = Marshal.ReadByte(ptr, count++);
                if (cur != 0x0)
                {
                    strBytes.Add(cur);
                }
                byte next = Marshal.ReadByte(ptr, count);

                // Reached end.
                if (next == 0x0 && cur == 0x0)
                {
                    break;
                }
            }

            return Encoding.ASCII.GetString(strBytes.ToArray());
        }

        /// <summary>
        /// Lire une chaine de caractères non managée en Unicode
        /// </summary>
        /// <param name="ptr">Pointeur vers la chaine de caractères</param>
        public static string ReadUnicodeNullTerminatedString(this IntPtr ptr)
        {
            List<byte> strBytes = [];
            int count = 0;
            byte cur;

            // Terminated by 2ishx \0
            while (true)
            {
                cur = Marshal.ReadByte(ptr, count++);
                if (cur != 0x0)
                {
                    strBytes.Add(cur);
                }
                byte next = Marshal.ReadByte(ptr, count);

                // Reached end.
                if (next == 0x0 && cur == 0x0)
                {
                    break;
                }
            }

            return Encoding.Unicode.GetString(strBytes.ToArray());
        }
    }
}
