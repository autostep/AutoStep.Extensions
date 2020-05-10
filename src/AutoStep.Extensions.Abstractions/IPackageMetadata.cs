using System.Collections.Generic;

namespace AutoStep.Extensions
{
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
        /// Gets the package category (from <see cref="PackageDependencyTypes"/>).
        /// </summary>
        string DependencyType { get; }

        /// <summary>
        /// Gets the set of dependent package IDs.
        /// </summary>
        IEnumerable<string> Dependencies { get; }

        /// <summary>
        /// Gets an absolute path within the package installation folder.
        /// </summary>
        /// <param name="pathParts">The parts of the relative path.</param>
        /// <returns>An absolute path.</returns>
        string GetPath(params string[] pathParts);
    }
}
