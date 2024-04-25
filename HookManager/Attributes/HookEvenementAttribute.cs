using System;

namespace HookManager.Attributes
{
    /// <summary>
    /// Attributs pour intercepter l'ajout et/ou la suppression d'un abonné à cet évènement (au démarrage, si <see cref="HookPool.InitializeAllAttributeHook(bool)"/> est appelée)
    /// </summary>
    [AttributeUsage(AttributeTargets.Event, AllowMultiple = false)]
    public class HookEvenementAttribute : HookManagerAttribute
    {
        /// <summary>
        /// Nom de la méthode interceptant l'ajout d'un abonné à cet évènement
        /// </summary>
        public string NomMethodeAjout { get; set; }

        /// <summary>
        /// Nom de la méthode interceptant l'ajout d'un abonné à cet évènement
        /// </summary>
        public string NomMethodeSupprime { get; set; }
    }
}
