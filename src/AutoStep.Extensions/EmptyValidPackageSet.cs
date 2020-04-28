using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    internal class EmptyValidPackageSet : IInstallablePackageSet
    {
        public static IInstallablePackageSet Instance { get; } = new EmptyValidPackageSet();

        public IEnumerable<string> PackageIds => Enumerable.Empty<string>();

        public bool IsValid => true;

        public Exception? Exception => null;

        public ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            return new ValueTask<InstalledExtensionPackages>(InstalledExtensionPackages.Empty);
        }
    }
}
