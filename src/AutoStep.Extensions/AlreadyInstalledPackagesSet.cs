using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions.NuGetExtensions
{
    /// <summary>
    /// Represents a package set where the packages have already been installed (i.e. there is no additional work to do at install time).
    /// </summary>
    internal class AlreadyInstalledPackagesSet : IInstallablePackageSet
    {
        private readonly InstalledExtensionPackages installedPackages;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlreadyInstalledPackagesSet"/> class.
        /// </summary>
        /// <param name="installedPackages">The set of packages.</param>
        public AlreadyInstalledPackagesSet(InstalledExtensionPackages installedPackages)
        {
            this.installedPackages = installedPackages;
        }

        /// <inheritdoc/>
        public IEnumerable<string> PackageIds => installedPackages.Packages.Select(x => x.PackageId);

        /// <inheritdoc/>
        public bool IsValid => true;

        /// <inheritdoc/>
        public Exception? Exception => null;

        /// <inheritdoc/>
        public ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            return new ValueTask<InstalledExtensionPackages>(installedPackages);
        }
    }
}
