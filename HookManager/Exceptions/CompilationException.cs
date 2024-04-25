using System;
using System.CodeDom.Compiler;
using System.Runtime.Serialization;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Exception when compile generated C# code
    /// </summary>
    [Serializable()]
    public class CompilationException : HookManagerException
    {
        private readonly CompilerErrorCollection _errors;

        /// <summary>
        /// Main constructors
        /// </summary>
        /// <param name="errors">Errors returned by C# compiler</param>
        public CompilationException(CompilerErrorCollection errors)
        {
            _errors = errors;
        }

        /// <summary>
        /// Errors from C# compiler
        /// </summary>
        public CompilerErrorCollection Errors
        {
            get { return _errors; }
        }

        /// <inheritdoc/>
        protected CompilationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
