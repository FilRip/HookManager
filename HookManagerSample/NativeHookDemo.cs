using System;
using System.Reflection;
using System.Runtime.InteropServices;

using HookManager;
using HookManager.Modeles;
using HookManager.Helpers;

namespace HookManagerSample
{
    public class NativeHookDemo
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int MessageBoxA(IntPtr hwnd, String text, String title, uint type);

        public void Main()
        {
            NativeHook nativeHook;

            // On substitue la méthode native user32.MessageBoxA. Elle a 2 signatures différentes selon si l'on est en 32 ou 64 bits
            if (Environment.Is64BitProcess)
            {
                MethodInfo nativeReplacementMethod = typeof(NativeHookDemo).GetMethod(nameof(MessageBoxReplacement64Bits), BindingFlags.Static | BindingFlags.NonPublic);
                nativeHook = HookPool.GetInstance().AjouterNativeHook(new NativeMethod("MessageBoxA", "user32.dll"), nativeReplacementMethod, false);
            }
            else
            {
                MethodInfo nativeReplacementMethod = typeof(NativeHookDemo).GetMethod(nameof(MessageBoxReplacement32Bits), BindingFlags.Static | BindingFlags.NonPublic);
                nativeHook = HookPool.GetInstance().AjouterNativeHook(new NativeMethod("MessageBoxA", "user32.dll"), nativeReplacementMethod, false);
            }

            if (nativeHook != null)
            {
                MessageBoxA(IntPtr.Zero, "Before Hook", "The title!", 0);

                nativeHook.Apply();
                MessageBoxA(IntPtr.Zero, "Modified Hello", "The title!", 0);
                nativeHook.Remove();

                MessageBoxA(IntPtr.Zero, "After Hook", "The title!", 0);
            }
        }

        private delegate int MessageBoxDelegate(IntPtr hwnd, string text, string title, uint type);
        private static int MessageBoxReplacement64Bits(IntPtr hwnd, IntPtr textPtr, IntPtr bodyPtr, uint type)
        {
            string titre = textPtr.ReadASCIINullTerminatedString();
            string texte = bodyPtr.ReadASCIINullTerminatedString();

            NativeHook nativeHook = HookPool.GetInstance().RetourneNativeHook("user32.dll", "MessageBoxA");
            if (nativeHook != null)
                return (int)nativeHook.Call<MessageBoxDelegate, object>(hwnd, "Hooked (original: " + texte + ")", titre, type);
            return -1;
        }

        // Warning désactivé. C'est normal, c'est pour les tests, cette nouvelle méthode, remplaçant l'ancienne, n'utilise pas tous les paramètres de l'ancienne
#pragma warning disable IDE0060
        private static int MessageBoxReplacement32Bits(int pad1, int pad2, uint type, IntPtr titlePtr, IntPtr bodyPtr, IntPtr hwnd)
#pragma warning restore IDE0060
        {
            string title = titlePtr.ReadASCIINullTerminatedString();
            string text = bodyPtr.ReadASCIINullTerminatedString();

            NativeHook nativeHook = HookPool.GetInstance().RetourneNativeHook("user32.dll", "MessageBoxA");
            if (nativeHook != null)
                return (int)nativeHook.Call<MessageBoxDelegate, object>(hwnd, "Hooked (original: " + text + ")", title, type); ;
            return -1;
        }
    }
}
