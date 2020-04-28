using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    internal interface IExtensionPackagesResolver
    {
        ValueTask<IInstallablePackageSet> ResolvePackagesAsync(IEnumerable<PackageExtensionConfiguration> extensions, CancellationToken cancelToken);
    }
}
