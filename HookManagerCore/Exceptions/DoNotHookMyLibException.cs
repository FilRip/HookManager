using System;
using System.Runtime.Serialization;

namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Ne pas substituer cet assembly (gérant la substitution)
    /// </summary>
    [Serializable()]
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

        /// <inheritdoc/>
        protected DoNotHookMyLibException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
