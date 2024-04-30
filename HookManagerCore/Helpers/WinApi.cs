using System.Runtime.InteropServices;

namespace HookManagerCore.Helpers
{
    internal static partial class WinApi
    {
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool VirtualProtect(IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial IntPtr GetProcAddress(IntPtr hModule, string procName);

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial IntPtr LoadLibraryExA(string lpFileName, IntPtr hReservedNull, uint dwFlags);
    }
}
