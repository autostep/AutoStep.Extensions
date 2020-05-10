using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Contains metadata related to a specific package.
    /// </summary>
    internal class PackageMetadata : IPackageMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageMetadata"/> class.
        /// </summary>
        /// <param name="packageId">The nuget package ID.</param>
        /// <param name="packageVersion">The installed package version.</param>
        /// <param name="packageFolder">An absolute path to the package's installed folder.</param>
        /// <param name="entryPoint">The relative path to an optional entry point assembly for the package.</param>
        /// <param name="libFiles">The set of all available library files.</param>
        /// <param name="dependencyType">Provides the package category (from <see cref="PackageDependencyTypes"/>).</param>
        /// <param name="dependencies">The set of package dependencies for this package.</param>
        public PackageMetadata(
            string packageId,
            string packageVersion,
            string packageFolder,
            string? entryPoint,
            IEnumerable<string> libFiles,
            string dependencyType,
            IEnumerable<string>? dependencies = null)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            PackageFolder = packageFolder;
            LibFiles = libFiles.ToList();
            EntryPoint = entryPoint;
            DependencyType = dependencyType;
            Dependencies = dependencies ?? Enumerable.Empty<string>();
        }

        /// <inheritdoc/>
        public string PackageFolder { get; }

        /// <inheritdoc/>
        public string PackageId { get; }

        /// <inheritdoc/>
        public string PackageVersion { get; }

        /// <inheritdoc/>
        public IReadOnlyList<string> LibFiles { get; }

        /// <inheritdoc/>
        public string? EntryPoint { get; }

        /// <inheritdoc/>
        public string DependencyType { get; }

        /// <inheritdoc/>
        public IEnumerable<string> Dependencies { get; }

        /// <inheritdoc/>
        public string GetPath(params string[] pathParts)
        {
            return Path.GetFullPath(Path.Combine(pathParts), PackageFolder);
        }
    }
}
