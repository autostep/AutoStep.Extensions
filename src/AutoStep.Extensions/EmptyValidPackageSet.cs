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
        private readonly IAutoStepEnvironment environment;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyValidPackageSet"/> class.
        /// </summary>
        /// <param name="environment">The environment block.</param>
        public EmptyValidPackageSet(IAutoStepEnvironment environment)
        {
            this.environment = environment;
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
            return new ValueTask<InstalledExtensionPackages>(
                new InstalledExtensionPackages(
                    environment,
                    Array.Empty<IPackageMetadata>()));
        }
    }
}
