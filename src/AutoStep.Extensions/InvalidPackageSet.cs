using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Represents an invalid package set.
    /// </summary>
    internal class InvalidPackageSet : IInstallablePackageSet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPackageSet"/> class.
        /// </summary>
        /// <param name="ex">An optional exception.</param>
        public InvalidPackageSet(Exception? ex = null)
        {
            Exception = ex;
        }

        /// <inheritdoc/>
        public IEnumerable<string> PackageIds => Array.Empty<string>();

        /// <inheritdoc/>
        public bool IsValid => false;

        /// <inheritdoc/>
        public Exception? Exception { get; }

        /// <inheritdoc/>
        public ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            throw new InvalidOperationException(Messages.InvalidPackageResolver_CannotInstallInvalidSet);
        }
    }
}
