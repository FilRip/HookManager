using System;
using System.Runtime.Serialization;

namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// A common exception happend
    /// </summary>
    [Serializable()]
    public class CommonHookManagerException : HookManagerException
    {
        /// <inheritdoc/>
        public CommonHookManagerException(string message) : base(message) { }

        /// <inheritdoc/>
        protected CommonHookManagerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
