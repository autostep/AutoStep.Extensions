using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    internal class CompositeExtensionResolver : IExtensionPackagesResolver
    {
        private readonly IExtensionPackagesResolver[] resolvers;

        public CompositeExtensionResolver(params IExtensionPackagesResolver[] resolvers)
        {
            this.resolvers = resolvers;
        }

        public async ValueTask<IInstallablePackageSet> ResolvePackagesAsync(IEnumerable<PackageExtensionConfiguration> extensions, CancellationToken cancelToken)
        {
            var resolved = new List<IInstallablePackageSet>();

            foreach (var resolver in resolvers)
            {
                cancelToken.ThrowIfCancellationRequested();

                resolved.Add(await resolver.ResolvePackagesAsync(extensions, cancelToken));
            }

            return new CompositeInstallablePackageSet(resolved);
        }
    }
}
