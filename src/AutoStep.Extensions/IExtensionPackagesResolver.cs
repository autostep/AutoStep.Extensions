using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines a service that can resolve a set of installable packages.
    /// </summary>
    internal interface IExtensionPackagesResolver
    {
        /// <summary>
        /// Resolve the set of packages this resolver can generate from the given <see cref="ExtensionResolveContext"/>.
        /// </summary>
        /// <param name="resolveContext">The resolve context (containing the configured extensions).</param>
        /// <param name="cancelToken">A cancellation token for the operation.</param>
        /// <returns>An async <see cref="ValueTask"/> that will complete with the installable package set.</returns>
        ValueTask<IInstallablePackageSet> ResolvePackagesAsync(ExtensionResolveContext resolveContext, CancellationToken cancelToken);
    }
}
