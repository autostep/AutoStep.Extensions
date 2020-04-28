using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    public interface IInstallablePackageSet
    {
        IEnumerable<string> PackageIds { get; }

        bool IsValid { get; }

        Exception? Exception { get; }

        ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken);
    }
}
