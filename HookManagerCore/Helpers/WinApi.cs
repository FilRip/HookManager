using System;
using System.Runtime.InteropServices;

namespace HookManagerCore.Helpers
{
    internal static class WinApi
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualProtect(IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);
    }
}
