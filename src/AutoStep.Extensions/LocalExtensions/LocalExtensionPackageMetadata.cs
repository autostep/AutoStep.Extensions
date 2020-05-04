using System.Collections.Generic;
using AutoStep.Extensions.Abstractions;

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
            : base(localPackage.PackageId, localPackage.ProjectVersion, packageFolder, entryPoint, libFiles, dependencyType, dependencies)
        {
            SourceProjectFile = localPackage.ProjectFile;
            SourceProjectFolder = localPackage.ProjectDirectory;
            SourceBinaryDirectory = localPackage.BinaryDirectory;
            SourceFiles = localPackage.SourceFiles;
            WatchMode = localPackage.WatchMode;
        }

        /// <inheritdoc/>
        public string SourceProjectFile { get; }

        /// <inheritdoc/>
        public string SourceProjectFolder { get; }

        /// <inheritdoc/>
        public IReadOnlyList<string> SourceFiles { get; }

        /// <inheritdoc/>
        public string SourceBinaryDirectory { get; }

        /// <inheritdoc/>
        public PackageWatchMode WatchMode { get; }
    }
}
