using System;
using System.Runtime.Serialization;

namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Erreur lors de la tentative de repaginer un espace mémoire d'après un pointeur mémoire
    /// </summary>
    [Serializable()]
    public class RepaginateMemoryException : HookManagerException
    {
        private readonly IntPtr _pointeurMemoire;

        /// <summary>
        /// Erreur lors de la tentative de repaginer un espace mémoire d'après un pointeur mémoire
        /// </summary>
        internal RepaginateMemoryException(IntPtr pointeurMemoire) : base()
        {
            _pointeurMemoire = pointeurMemoire;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Error in repaginate memory. Unable to access to memory at address 0x{_pointeurMemoire.ToInt32():X16}";
            }
        }

        /// <summary>
        /// Pointeur vers la mémoire en cause
        /// </summary>
        public IntPtr MemoryPtr
        {
            get { return _pointeurMemoire; }
        }

        /// <inheritdoc/>
        protected RepaginateMemoryException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(MemoryPtr), _pointeurMemoire);
        }
    }
}
