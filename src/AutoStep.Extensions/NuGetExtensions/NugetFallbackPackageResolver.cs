using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AutoStep.Extensions.NuGetExtensions
{
    /// <summary>
    /// A package resolver that attempts to use the <see cref="CachedPackagesResolver"/>, and falls back to the <see cref="NugetPackagesResolver"/>
    /// if the cache is not valid.
    /// </summary>
    internal class NugetFallbackPackageResolver : IExtensionPackagesResolver
    {
        private const string ExtensionDependencyFile = "extensions.deps.json";
        private readonly string dependencyJsonFile;
        private readonly ISourceSettings sourceSettings;
        private readonly IHostContext hostContext;
        private readonly bool noCache;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NugetFallbackPackageResolver"/> class.
        /// </summary>
        /// <param name="sourceSettings">The nuget source settings.</param>
        /// <param name="hostContext">The host context.</param>
        /// <param name="noCache">Indicates whether or not to use the package cache.</param>
        /// <param name="logger">A logger.</param>
        public NugetFallbackPackageResolver(
            ISourceSettings sourceSettings,
            IHostContext hostContext,
            bool noCache,
            ILogger logger)
        {
            // Determine the full dependency file.
            dependencyJsonFile = Path.Combine(hostContext.ExtensionsDirectory, ExtensionDependencyFile);
            this.sourceSettings = sourceSettings;
            this.hostContext = hostContext;
            this.noCache = noCache;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async ValueTask<IInstallablePackageSet> ResolvePackagesAsync(ExtensionResolveContext resolveContext, CancellationToken cancelToken)
        {
            if (!noCache)
            {
                // Try using the cached set of package information.
                var cachedLoader = new CachedPackagesResolver(hostContext, dependencyJsonFile, logger);

                var cachedPackages = await cachedLoader.ResolvePackagesAsync(resolveContext, cancelToken).ConfigureAwait(false);

                if (cachedPackages.IsValid)
                {
                    // Cache is good, use it.
                    return cachedPackages;
                }
            }

            // Cache won't work.
            var nugetResolver = new NugetPackagesResolver(sourceSettings, hostContext, noCache, logger);

            var nugetLoadedPackages = await nugetResolver.ResolvePackagesAsync(resolveContext, cancelToken).ConfigureAwait(false);

            if (!nugetLoadedPackages.IsValid)
            {
                return nugetLoadedPackages;
            }

            // Wrap the package set with one that updates the deps.json file.
            return new WriteCacheOnInstallPackageSet(dependencyJsonFile, hostContext, resolveContext, nugetLoadedPackages);
        }
    }
}
