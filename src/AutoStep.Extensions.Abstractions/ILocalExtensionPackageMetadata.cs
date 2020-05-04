using System.Collections.Generic;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines additional metadata for local extensions.
    /// </summary>
    public interface ILocalExtensionPackageMetadata : IPackageMetadata
    {
        /// <summary>
        /// Gets the absolute path to the project file.
        /// </summary>
        string SourceProjectFile { get; }

        /// <summary>
        /// Gets the absolute path to the folder the project was loaded from.
        /// </summary>
        string SourceProjectFolder { get; }

        /// <summary>
        /// Gets the absolute path to the binary directory that the package was copied from.
        /// </summary>
        string SourceBinaryDirectory { get; }

        /// <summary>
        /// Gets the set of source files in the project. Will be empty unless <see cref="WatchMode"/> is <see cref="PackageWatchMode.Full"/>.
        /// </summary>
        IReadOnlyList<string> SourceFiles { get; }

        /// <summary>
        /// Gets the watch mode for the local package.
        /// </summary>
        PackageWatchMode WatchMode { get; }
    }
}
