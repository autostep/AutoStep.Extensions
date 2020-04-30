using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines a set of packages that can be installed on-demand.
    /// </summary>
    public interface IInstallablePackageSet
    {
        /// <summary>
        /// Gets the set of package IDs that will be installed.
        /// </summary>
        IEnumerable<string> PackageIds { get; }

        /// <summary>
        /// Gets a value indicating whether the set is 'valid'. Calling <see cref="InstallAsync"/> on a set where
        /// <see cref="IsValid"/> is false will result in an InvalidOperationException.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Gets a caught exception raised during package resolution. If this value is set, <see cref="IsValid"/> will be false,
        /// but that statement is not necessarily true the other way round.
        /// </summary>
        Exception? Exception { get; }

        /// <summary>
        /// Install the set of packages, yielding a set of installed packages.
        /// </summary>
        /// <param name="cancelToken">A cancellation token.</param>
        /// <returns>An awaitable task that will complete when the packages are installed, and result in the set of installed packages.</returns>
        ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken);
    }
}
