using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    internal class CachedInstallablePackagesSet : IInstallablePackageSet
    {
        private readonly InstalledExtensionPackages cachedPackages;

        public CachedInstallablePackagesSet(InstalledExtensionPackages cachedPackages)
        {
            this.cachedPackages = cachedPackages;
        }

        public IEnumerable<string> PackageIds => cachedPackages.Packages.Select(x => x.PackageId);

        public bool IsValid => true;

        public Exception? Exception => null;

        public ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            return new ValueTask<InstalledExtensionPackages>(cachedPackages);
        }
    }
}
