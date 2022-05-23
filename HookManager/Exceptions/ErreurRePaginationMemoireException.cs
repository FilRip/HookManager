using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur lors de la tentative de repaginer un espace mémoire d'après un pointeur mémoire
    /// </summary>
    [Serializable()]
    public class ErreurRePaginationMemoireException : HookManagerException
    {
        private IntPtr _pointeurMemoire;

        /// <summary>
        /// Erreur lors de la tentative de repaginer un espace mémoire d'après un pointeur mémoire
        /// </summary>
        internal ErreurRePaginationMemoireException(IntPtr pointeurMemoire) : base()
        {
            _pointeurMemoire = pointeurMemoire;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Erreur de repagination de la mémoire. Impossible d'accéder à la mémoire 0x{_pointeurMemoire.ToInt32():X16}";
            }
        }

        /// <summary>
        /// Pointeur vers la mémoire en cause
        /// </summary>
        public IntPtr PointeurMemoire
        {
            get { return _pointeurMemoire; }
        }
    }
}
