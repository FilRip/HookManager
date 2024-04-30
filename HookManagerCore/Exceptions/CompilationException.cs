using Microsoft.CodeAnalysis;

namespace HookManagerCore.Exceptions
{
    /// <summary>
    /// Exception when compile generated C# code
    /// </summary>
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
    }
}
