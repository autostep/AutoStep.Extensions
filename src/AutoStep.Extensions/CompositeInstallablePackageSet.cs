using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    internal class CompositeInstallablePackageSet : IInstallablePackageSet
    {
        private readonly IEnumerable<IInstallablePackageSet> installableSets;

        public CompositeInstallablePackageSet(IEnumerable<IInstallablePackageSet> installableSets)
        {
            this.installableSets = installableSets;

            IEnumerable<Exception> exceptions = installableSets.Select(x => x.Exception).Where(ex => ex != null)!;

            if (exceptions.Any())
            {
                Exception = new AggregateException(exceptions);
            }
        }

        public IEnumerable<string> PackageIds => installableSets.SelectMany(i => i.PackageIds);

        public bool IsValid => installableSets.All(i => i.IsValid);

        public Exception? Exception { get; }

        public async ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("Cannot install invalid package set.");
            }

            var allPackages = new List<IPackageMetadata>();

            foreach (var installable in installableSets)
            {
                var packages = await installable.InstallAsync(cancelToken);

                allPackages.AddRange(packages.Packages);
            }

            return new InstalledExtensionPackages(allPackages);
        }
    }
}
