namespace HookManagerCore.Modeles
{
    /// <summary>
    /// Classe représentant la méthode native d'origine
    /// </summary>
    /// <remarks>
    /// Objet représentant la méthode Native à substituer
    /// </remarks>
    /// <param name="method">Nom de la méthode</param>
    /// <param name="module">Nom du module (avec son extension)</param>
    public class NativeMethod(string method, string module)
    {
        #region Fields

        /// <summary>
        /// Le pointeur (adresse mémoire) de la méthode Native
        /// </summary>
        public IntPtr Address { get; set; }

        /// <summary>
        /// Le nom de la méthode
        /// </summary>
        public string Method { get; set; } = method;

        /// <summary>
        /// Le nom du module (avec son extension)
        /// </summary>
        public string ModuleName { get; set; } = module;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ModuleName} - {Method} ({Address})";
        }

        #endregion
    }
}
