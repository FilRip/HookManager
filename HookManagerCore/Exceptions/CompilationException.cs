using System.Runtime.Serialization;

using Microsoft.CodeAnalysis;

namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Exception when compile generated C# code
    /// </summary>
    [Serializable()]
    public class CompilationException : HookManagerException
    {
        private readonly IEnumerable<Diagnostic> _errors;

        /// <summary>
        /// Main constructors
        /// </summary>
        /// <param name="errors">Errors returned by C# compiler</param>
        public CompilationException(IEnumerable<Diagnostic> errors)
        {
            _errors = errors;
        }

        /// <summary>
        /// Errors from C# compiler
        /// </summary>
        public IEnumerable<Diagnostic> Errors
        {
            get { return _errors; }
        }

        /// <inheritdoc/>
        protected CompilationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
