using System;
using System.IO;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Contains an AutoStep Environment.
    /// </summary>
    public class AutoStepEnvironment : IAutoStepEnvironment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AutoStepEnvironment"/> class.
        /// </summary>
        /// <param name="rootDirectory">The project root directory.</param>
        /// <param name="extensionsDirectory">The directory to install extensions in.</param>
        public AutoStepEnvironment(string rootDirectory, string extensionsDirectory)
        {
            if (!Path.IsPathFullyQualified(rootDirectory))
            {
                throw new ArgumentException(Messages.DirectoryMustBeAbsolute, nameof(rootDirectory));
            }

            if (!Path.IsPathFullyQualified(extensionsDirectory))
            {
                throw new ArgumentException(Messages.DirectoryMustBeAbsolute, nameof(extensionsDirectory));
            }

            RootDirectory = rootDirectory;
            ExtensionsDirectory = extensionsDirectory;
        }

        /// <inheritdoc/>
        public string RootDirectory { get; }

        /// <inheritdoc/>
        public string ExtensionsDirectory { get; }
    }
}
