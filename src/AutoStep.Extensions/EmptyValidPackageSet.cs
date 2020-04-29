using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines a singleton package set representing an empty (but valid package set).
    /// </summary>
    internal class EmptyValidPackageSet : IInstallablePackageSet
    {
        /// <summary>
        /// Gets the singleton instance of the empty package set.
        /// </summary>
        public static IInstallablePackageSet Instance { get; } = new EmptyValidPackageSet();

        private EmptyValidPackageSet()
        {
        }

        /// <inheritdoc/>
        public IEnumerable<string> PackageIds => Enumerable.Empty<string>();

        /// <inheritdoc/>
        public bool IsValid => true;

        /// <inheritdoc/>
        public Exception? Exception => null;

        /// <inheritdoc/>
        public ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            return new ValueTask<InstalledExtensionPackages>(InstalledExtensionPackages.Empty);
        }
    }
}
