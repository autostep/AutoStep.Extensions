using System.Collections.Generic;
using NuGet.Packaging.Core;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Context object for the extension resolve project.
    /// </summary>
    internal class ExtensionResolveContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionResolveContext"/> class.
        /// </summary>
        /// <param name="packageExtensions">The configured set of extensions loaded from NuGet.</param>
        /// <param name="folderExtensions">The configured set of extensions loaded from folders.</param>
        public ExtensionResolveContext(IEnumerable<PackageExtensionConfiguration> packageExtensions, IEnumerable<FolderExtensionConfiguration> folderExtensions)
        {
            PackageExtensions = packageExtensions;
            FolderExtensions = folderExtensions;
            AdditionalPackagesRequired = new List<PackageDependency>();
        }

        /// <summary>
        /// Gets the set of configured nuget package extensions.
        /// </summary>
        public IEnumerable<PackageExtensionConfiguration> PackageExtensions { get; }

        /// <summary>
        /// Gets the set of configured folder extensions.
        /// </summary>
        public IEnumerable<FolderExtensionConfiguration> FolderExtensions { get; }

        /// <summary>
        /// Gets the list of additional packages that are required. A resolver can add to this list to tell the NuGet package resolver
        /// to include those dependencies in the final set.
        /// </summary>
        public List<PackageDependency> AdditionalPackagesRequired { get; }
    }
}
