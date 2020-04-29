using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace AutoStep.Extensions.NuGetExtensions
{
    internal class NugetFallbackPackageResolver : IExtensionPackagesResolver
    {
        private const string ExtensionDependencyFile = "extensions.deps.json";
        private readonly string dependencyJsonFile;
        private readonly ISourceSettings sourceSettings;
        private readonly IHostContext hostContext;
        private readonly bool noCache;
        private readonly ILogger logger;

        public NugetFallbackPackageResolver(
            ISourceSettings sourceSettings,
            IHostContext hostContext,
            bool noCache,
            ILogger logger)
        {
            // Ensure that the extensions folder exists.
            dependencyJsonFile = Path.Combine(hostContext.ExtensionsDirectory, ExtensionDependencyFile);
            this.sourceSettings = sourceSettings;
            this.hostContext = hostContext;
            this.noCache = noCache;
            this.logger = logger;
        }

        public async ValueTask<IInstallablePackageSet> ResolvePackagesAsync(ExtensionResolveContext resolveContext, CancellationToken cancelToken)
        {
            // Take a lock on the dependency file.
            if (!noCache && File.Exists(dependencyJsonFile))
            {
                // Load the dependency context.
                DependencyContext? cacheDepCtxt = LoadCachedDependencyInfo();

                if (cacheDepCtxt is object)
                {
                    var cachedLoader = new CachedPackagesResolver(hostContext, cacheDepCtxt, logger);

                    var cachedPackages = await cachedLoader.ResolvePackagesAsync(resolveContext, cancelToken).ConfigureAwait(false);

                    if (cachedPackages.IsValid)
                    {
                        return cachedPackages;
                    }
                }
            }

            var nugetResolver = new NugetPackagesResolver(sourceSettings, hostContext, noCache, logger);

            var nugetLoadedPackages = await nugetResolver.ResolvePackagesAsync(resolveContext, cancelToken).ConfigureAwait(false);

            if (!nugetLoadedPackages.IsValid)
            {
                return nugetLoadedPackages;
            }

            // Wrap the package set with one that updates the package cache.
            return new CacheOnInstallPackageSet(dependencyJsonFile, hostContext, resolveContext, nugetLoadedPackages);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Not sure what exceptions will be thrown by dependency context loader.")]
        private DependencyContext? LoadCachedDependencyInfo()
        {
            try
            {
                using var dependencyContextLdr = new DependencyContextJsonReader();
                using var stream = File.OpenRead(dependencyJsonFile);

                return dependencyContextLdr.Read(stream);
            }
            catch (Exception ex)
            {
                // Corrupt file, IO error? Lets just assume we can't get the info.
                logger.LogWarning(ex, Messages.ExtensionSetLoader_CorruptExtensionCacheFile);
            }

            return null;
        }
    }
}
