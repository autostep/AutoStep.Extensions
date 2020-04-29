using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Provides an extension packages resolver that checks multiple wrapped resolvers, and returns an composite package set.
    /// </summary>
    internal class CompositeExtensionResolver : IExtensionPackagesResolver
    {
        private readonly IExtensionPackagesResolver[] resolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeExtensionResolver"/> class.
        /// </summary>
        /// <param name="resolvers">The set opf wrapped resolvers.</param>
        public CompositeExtensionResolver(params IExtensionPackagesResolver[] resolvers)
        {
            this.resolvers = resolvers;
        }

        /// <inheritdoc/>
        public async ValueTask<IInstallablePackageSet> ResolvePackagesAsync(ExtensionResolveContext resolveContext, CancellationToken cancelToken)
        {
            var resolved = new LinkedList<IInstallablePackageSet>();

            foreach (var resolver in resolvers)
            {
                cancelToken.ThrowIfCancellationRequested();

                // Adding to the start of the list, because we want to install in the reverse order of resolving.
                // This is to ensure that the last resolution stage, that resolves all additional package dependencies, installs it's packages first.
                resolved.AddFirst(await resolver.ResolvePackagesAsync(resolveContext, cancelToken));
            }

            return new CompositeInstallablePackageSet(resolved);
        }
    }
}
