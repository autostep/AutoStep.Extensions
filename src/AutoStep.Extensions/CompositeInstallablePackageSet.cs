using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Represents an installable package set that wraps multiple contained sets.
    /// </summary>
    internal class CompositeInstallablePackageSet : IInstallablePackageSet
    {
        private readonly IEnumerable<IInstallablePackageSet> installableSets;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeInstallablePackageSet"/> class.
        /// </summary>
        /// <param name="installableSets">The collection of installable sets wrapped by this composite set.</param>
        public CompositeInstallablePackageSet(IEnumerable<IInstallablePackageSet> installableSets)
        {
            this.installableSets = installableSets;

            IEnumerable<Exception> exceptions = installableSets.Select(x => x.Exception).Where(ex => ex != null)!;

            if (exceptions.Any())
            {
                Exception = new AggregateException(exceptions);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> PackageIds => installableSets.SelectMany(i => i.PackageIds);

        /// <inheritdoc/>
        public bool IsValid => installableSets.All(i => i.IsValid);

        /// <inheritdoc/>
        public Exception? Exception { get; }

        /// <inheritdoc/>
        public async ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(Messages.InvalidPackageResolver_CannotInstallInvalidSet);
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
