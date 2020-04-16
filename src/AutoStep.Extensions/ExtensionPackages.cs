using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Contains the set of installed packages.
    /// </summary>
    internal class ExtensionPackages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionPackages"/> class.
        /// </summary>
        /// <param name="packagesRootDir">The root directory for all packages.</param>
        /// <param name="packages">The set of package metadata.</param>
        public ExtensionPackages(string packagesRootDir, IReadOnlyList<PackageMetadata> packages)
        {
            PackagesRootDir = packagesRootDir;
            Packages = packages;
        }

        /// <summary>
        /// Gets the root directory for all packages.
        /// </summary>
        public string PackagesRootDir { get; }

        /// <summary>
        /// Gets the set of loaded package metadata.
        /// </summary>
        public IReadOnlyList<PackageMetadata> Packages { get; }

        /// <summary>
        /// Get all packages that are considered top-level (i.e. were originally requested as configured extensions).
        /// </summary>
        /// <returns>The filtered package set.</returns>
        public IEnumerable<PackageMetadata> GetTopLevelPackages()
        {
            return Packages.Where(x => x.IsTopLevel);
        }
    }
}
