namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Ne pas substituer cet assembly (gérant la substitution)
    /// </summary>
    public class DoNotHookMyLibException : HookManagerException
    {
        internal DoNotHookMyLibException() : base()
        {
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return "La substitution d'élément de cette librairie est interdit, fortement déconseillé car risque d'instabilité du système";
            }
        }
    }
}
