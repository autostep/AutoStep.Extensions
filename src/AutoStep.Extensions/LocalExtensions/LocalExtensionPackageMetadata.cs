using System.Collections.Generic;

namespace AutoStep.Extensions.LocalExtensions
{
    /// <summary>
    /// Provides the local extension metadata.
    /// </summary>
    internal class LocalExtensionPackageMetadata : PackageMetadata, ILocalExtensionPackageMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalExtensionPackageMetadata"/> class.
        /// </summary>
        /// <param name="localPackage">The local project package data.</param>
        /// <param name="packageFolder">An absolute path to the package's installed folder.</param>
        /// <param name="entryPoint">The relative path to an optional entry point assembly for the package.</param>
        /// <param name="libFiles">The set of all available library files.</param>
        /// <param name="dependencyType">Provides the package category (from <see cref="PackageDependencyTypes"/>).</param>
        /// <param name="dependencies">The set of package dependencies for this package.</param>
        public LocalExtensionPackageMetadata(
            LocalProjectPackage localPackage,
            string packageFolder,
            string? entryPoint,
            IEnumerable<string> libFiles,
            string dependencyType,
            IEnumerable<string>? dependencies = null)
            : base(localPackage.ProjectName, localPackage.ProjectVersion, packageFolder, entryPoint, libFiles, dependencyType, dependencies)
        {
            SourceProjectFolder = localPackage.ProjectDirectory;
            SourceBinaryDirectory = localPackage.BinaryDirectory;
        }

        /// <inheritdoc/>
        public string SourceProjectFolder { get; }

        /// <inheritdoc/>
        public string SourceBinaryDirectory { get; }
    }
}
