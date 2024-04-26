using System;
using System.Diagnostics;
using System.Reflection;

namespace HookManagerCore.Helpers
{
    /// <summary>
    /// Classe contenant des méthodes d'extensions des Assembly
    /// </summary>
    public static class ExtensionsAssembly
    {
        /// <summary>
        /// Détermine si l'assembly a été compilée avec l'option "JIT Optimized" activée
        /// </summary>
        /// <param name="asm">Assembly à vérifier</param>
        public static bool IsJITOptimizerEnabled(this Assembly asm)
        {
            object[] debugAttrib;
            debugAttrib = asm.GetCustomAttributes(typeof(DebuggableAttribute), false);
            if ((debugAttrib != null) && (debugAttrib.Length > 0))
                return !((DebuggableAttribute)debugAttrib[0]).IsJITOptimizerDisabled;
            return false;
        }

        internal static Type SearchType(this Assembly[] listeAssembly, string nomType)
        {
            foreach (Assembly ass in listeAssembly)
                if (ass.GetType(nomType, false) != null)
                    return ass.GetType(nomType, false);
            return null;
        }
    }
}
