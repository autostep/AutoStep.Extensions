using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    internal class InvalidPackageSet : IInstallablePackageSet
    {
        public InvalidPackageSet(Exception? ex = null)
        {
            Exception = ex;
        }

        public IEnumerable<string> PackageIds => Array.Empty<string>();

        public bool IsValid => false;

        public Exception? Exception { get; }

        public ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            throw new InvalidOperationException("Cannot install invalid package set.");
        }
    }
}
