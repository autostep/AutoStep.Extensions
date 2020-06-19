using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace AutoStep.Extensions.NuGetExtensions
{
    /// <summary>
    /// Extension package resolver for NuGet Packages.
    /// </summary>
    internal class NugetPackagesResolver : IExtensionPackagesResolver
    {
        private readonly ISourceSettings sourceSettings;
        private readonly IHostContext hostContext;
        private readonly bool noCache;
        private readonly ILogger logger;
        private readonly IEnumerable<SourceRepository> repositories;

        /// <summary>
        /// Initializes a new instance of the <see cref="NugetPackagesResolver"/> class.
        /// </summary>
        /// <param name="sourceSettings">The nuget source settings.</param>
        /// <param name="hostContext">The host context.</param>
        /// <param name="noCache">If true, do not use the nuget cache.</param>
        /// <param name="logger">A logger.</param>
        public NugetPackagesResolver(
            ISourceSettings sourceSettings,
            IHostContext hostContext,
            bool noCache,
            Microsoft.Extensions.Logging.ILogger logger)
        {
            this.sourceSettings = sourceSettings;
            this.hostContext = hostContext;
            this.noCache = noCache;
            this.logger = new NuGetLogger(logger);

            // Establish the source repository provider; the available providers come from our custom settings.
            var sourceRepositoryProvider = new SourceRepositoryProvider(sourceSettings.NugetSourceProvider, Repository.Provider.GetCoreV3());

            repositories = sourceRepositoryProvider.GetRepositories();
        }

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Any nuget load problem should be caught.")]
        public async ValueTask<IInstallablePackageSet> ResolvePackagesAsync(ExtensionResolveContext resolveContext, CancellationToken cancelToken)
        {
            if (!resolveContext.PackageExtensions.Any() && resolveContext.AdditionalPackagesRequired.Count == 0)
            {
                // Nothing to do, no packages to install.
                return new EmptyValidPackageSet(hostContext.Environment);
            }

            using var cacheContext = new SourceCacheContext();

            if (noCache)
            {
                cacheContext.MaxAge = DateTimeOffset.UtcNow;
            }

            try
            {
                // The available packages list will contain the set of all packages to use.
                var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);

                var packageExtensionIds = new HashSet<string>();
                var additionalPackageIds = new HashSet<string>();

                var allPackageIds = new List<string>();

                foreach (var package in resolveContext.PackageExtensions)
                {
                    // Determine the correct version of the package to use.
                    var packageIdentity = await GetPackageIdentity(package, cacheContext, repositories, cancelToken).ConfigureAwait(false);

                    if (packageIdentity is null)
                    {
                        throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.NuGetPackagesResolver_ExtensionNotFound, package.Package));
                    }

                    // Configured extensions make up our 'target' packages.
                    packageExtensionIds.Add(packageIdentity.Id);
                    allPackageIds.Add(packageIdentity.Id);

                    // Search the graph of all the package dependencies to get the full set of available packages.
                    await GetPackageDependencies(packageIdentity, cacheContext, logger, repositories, availablePackages, cancelToken).ConfigureAwait(false);
                }

                foreach (var additionalPackage in resolveContext.AdditionalPackagesRequired)
                {
                    // Determine the correct version of the package to use.
                    var packageIdentity = await GetPackageIdentity(additionalPackage.Id, additionalPackage.VersionRange, true, cacheContext, repositories, cancelToken).ConfigureAwait(false);

                    if (packageIdentity is null)
                    {
                        throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.NuGetPackagesResolver_AdditionalDependencyNotFound, additionalPackage.Id));
                    }

                    additionalPackageIds.Add(packageIdentity.Id);
                    allPackageIds.Add(packageIdentity.Id);

                    // Search the graph of all the package dependencies to get the full set of available packages.
                    await GetPackageDependencies(packageIdentity, cacheContext, logger, repositories, availablePackages, cancelToken).ConfigureAwait(false);
                }

                // Create a package resolver context (this is used to help figure out which actual package versions to install).
                var resolverContext = new PackageResolverContext(
                       DependencyBehavior.Lowest,
                       allPackageIds,
                       packageExtensionIds,
                       Enumerable.Empty<PackageReference>(),
                       Enumerable.Empty<PackageIdentity>(),
                       availablePackages,
                       repositories.Select(s => s.PackageSource),
                       logger);

                var resolver = new PackageResolver();

                // Work out the actual set of packages to install.
                var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                                                .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));

                return new NugetInstallablePackageSet(sourceSettings, hostContext, packagesToInstall, packageExtensionIds, additionalPackageIds, noCache, logger);
            }
            catch (Exception ex)
            {
                return new InvalidPackageSet(ex);
            }
        }

        /// <summary>
        /// Retrieves the actual package identity to install for an extension, given the configuration (based on requested version specifier, pre-release, etc).
        /// </summary>
        private Task<PackageIdentity?> GetPackageIdentity(PackageExtensionConfiguration extConfig, SourceCacheContext cache, IEnumerable<SourceRepository> repositories, CancellationToken cancelToken)
        {
            VersionRange? range = null;

            if (extConfig.Version is object)
            {
                if (!VersionRange.TryParse(extConfig.Version, out range))
                {
                    throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.NuGetPackagesResolver_BadVersionRange, extConfig.Package));
                }
            }

            return GetPackageIdentity(extConfig.Package!, range, extConfig.PreRelease, cache, repositories, cancelToken);
        }

        /// <summary>
        /// Retrieves the actual package identity to install for an extension, given the configuration (based on requested version specifier, pre-release, etc).
        /// </summary>
        private async Task<PackageIdentity?> GetPackageIdentity(string packageId, VersionRange? versionRange, bool permitPreRelease, SourceCacheContext cache, IEnumerable<SourceRepository> repositories, CancellationToken cancelToken)
        {
            PackageIdentity? selectedPackageId = null;

            // Go through each repository.
            // If a repository contains only pre-release packages (e.g. AutoStep CI), and the configuration doesn't permit pre-release versions,
            // the search will look at other ones (e.g. NuGet).
            foreach (var sourceRepository in repositories)
            {
                var findPackageResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);

                var allVersions = (await findPackageResource.GetAllVersionsAsync(packageId, cache, logger, cancelToken).ConfigureAwait(false)).ToList();

                NuGetVersion? selected;

                // Have we specified a version range?
                if (versionRange != null)
                {
                    // Find the best package version match for the range.
                    // Consider pre-release versions, but only if the extension is configured to use them.
                    var bestVersion = versionRange.FindBestMatch(allVersions.Where(v => permitPreRelease || !v.IsPrerelease));

                    selected = bestVersion;
                }
                else
                {
                    // No version; choose the latest, allow pre-release if configured.
                    selected = allVersions.LastOrDefault(v => permitPreRelease || !v.IsPrerelease);
                }

                if (selected is object)
                {
                    if (selectedPackageId is null)
                    {
                        selectedPackageId = new PackageIdentity(packageId, selected);
                    }
                    else if (versionRange is null)
                    {
                        if (selected > selectedPackageId.Version)
                        {
                            selectedPackageId = new PackageIdentity(packageId, selected);
                        }
                    }
                    else if (versionRange.IsBetter(selectedPackageId.Version, selected))
                    {
                        selectedPackageId = new PackageIdentity(packageId, selected);
                    }
                }
            }

            return selectedPackageId;
        }

        /// <summary>
        /// Searches the package dependency graph for the chain of all packages to install.
        /// </summary>
        private async Task GetPackageDependencies(
                PackageIdentity package,
                SourceCacheContext cacheContext,
                ILogger logger,
                IEnumerable<SourceRepository> repositories,
                ISet<SourcePackageDependencyInfo> availablePackages,
                CancellationToken cancelToken)
        {
            // Don't recurse over a package we've already seen.
            if (availablePackages.Contains(package))
            {
                return;
            }

            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>().ConfigureAwait(false);
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package,
                    hostContext.TargetFramework,
                    cacheContext,
                    logger,
                    cancelToken).ConfigureAwait(false);

                // No info for the package in this repository.
                if (dependencyInfo == null)
                {
                    continue;
                }

                // Filter the dependency info.
                // Don't bring in any dependencies that are provided by the host.
                var actualSourceDep = new SourcePackageDependencyInfo(
                    dependencyInfo.Id,
                    dependencyInfo.Version,
                    dependencyInfo.Dependencies.Where(dep => !hostContext.DependencySuppliedByHost(dep)),
                    dependencyInfo.Listed,
                    dependencyInfo.Source);

                availablePackages.Add(actualSourceDep);

                foreach (var dependency in actualSourceDep.Dependencies)
                {
                    await GetPackageDependencies(
                        new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                        cacheContext,
                        logger,
                        repositories,
                        availablePackages,
                        cancelToken).ConfigureAwait(false);
                }

                break;
            }
        }
    }
}
