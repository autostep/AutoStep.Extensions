using System.Collections.Generic;

namespace AutoStep.Extensions
{
    public interface IPackageDependency
    {
        string PackageId { get; }

        string PackageVersion { get; }
    }

    /// <summary>
    /// Defines available metadata for a package.
    /// </summary>
    public interface IPackageMetadata
    {
        /// <summary>
        /// Gets the absolute path of the package's installed folder.
        /// </summary>
        string PackageFolder { get; }

        /// <summary>
        /// Gets the package ID.
        /// </summary>
        string PackageId { get; }

        /// <summary>
        /// Gets the installed package version.
        /// </summary>
        string PackageVersion { get; }

        /// <summary>
        /// Gets the set of available DLL files.
        /// </summary>
        IReadOnlyList<string> LibFiles { get; }

        /// <summary>
        /// Gets the relative path to an optional entry point assembly for the package.
        /// </summary>
        string? EntryPoint { get; }

        /// <summary>
        /// Gets a value indicating whether the specified package is a top-level dependency (as opposed to a nested chain dependency).
        /// </summary>
        bool IsTopLevel { get; }

        IEnumerable<string> Dependencies { get; }
    }
}
