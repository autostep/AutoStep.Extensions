using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Represents an exception that occurred while loading extensions.
    /// </summary>
    public class ExtensionLoadException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionLoadException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ExtensionLoadException(string? message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionLoadException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The underlying error.</param>
        public ExtensionLoadException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
