namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// A common exception happend
    /// </summary>
    public class CommonHookManagerException : HookManagerException
    {
        /// <inheritdoc/>
        public CommonHookManagerException(string message) : base(message) { }
    }
}
