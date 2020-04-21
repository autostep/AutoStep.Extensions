using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Provides the functionality to load extensions from nuget.
    /// </summary>
    /// <remarks>
    /// Credit due to the post from Martin Bjorkstrom (https://martinbjorkstrom.com/posts/2018-09-19-revisiting-nuget-client-libraries),
    /// and associated gist (https://gist.github.com/mholo65/ad5776c36559410f45d5dcd0181a5c64) that helped provide the starting point for this code.
    /// </remarks>
    internal class NuGetPackagesLoader
    {
        private readonly NuGet.Common.ILogger nugetLogger;
        private readonly string packageDirectory;
        private readonly IHostContext hostContext;
        private readonly string? entryPointPackageTag;

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetPackagesLoader"/> class.
        /// </summary>
        /// <param name="extensionsFolder">The folder to install extensions in.</param>
        /// <param name="hostContext">The hosting context.</param>
        /// <param name="entryPointPackageTag">An optional tag name to filter loaded packages considered as possible 'entry points' for extensions.</param>
        /// <param name="logger">A logger for the restore process.</param>
        public NuGetPackagesLoader(string extensionsFolder, IHostContext hostContext, string? entryPointPackageTag, ILogger logger)
        {
            nugetLogger = new NuGetLogger(logger);

            packageDirectory = extensionsFolder;
            this.hostContext = hostContext;
            this.entryPointPackageTag = entryPointPackageTag;
        }

        /// <summary>
        /// Resolve the set of extension packages.
        /// </summary>
        /// <param name="sourceSettings">The source settings (defines where to load extensions and their dependencies from).</param>
        /// <param name="configuredExtensions">The extensions to install.</param>
        /// <param name="noCache">True to ignore the NuGet cache.</param>
        /// <param name="cancelToken">Cancellation token for stopping.</param>
        /// <returns>An awaitable task that will contain the set of loaded packages.</returns>
        public async Task<ExtensionPackages> LoadExtensionPackagesAsync(ISourceSettings sourceSettings, IEnumerable<ExtensionConfiguration> configuredExtensions, bool noCache, CancellationToken cancelToken)
        {
            // Establish the source repository provider; the available providers come from our custom settings.
            var sourceRepositoryProvider = new SourceRepositoryProvider(sourceSettings.SourceProvider, Repository.Provider.GetCoreV3());

            var repositories = sourceRepositoryProvider.GetRepositories();

            // Disposable source cache.
            using var sourceCacheContext = new SourceCacheContext();

            if (noCache)
            {
                sourceCacheContext.MaxAge = DateTimeOffset.UtcNow;
            }

            // The available packages list will contain the set of all packages to use.
            var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
            var listTargetIds = new List<string>();

            foreach (var package in configuredExtensions)
            {
                // Determine the correct version of the package to use.
                var packageIdentity = await GetPackageIdentity(package, sourceCacheContext, repositories, cancelToken).ConfigureAwait(false);

                if (packageIdentity is null)
                {
                    throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.NuGetPackagesLoader_ExtensionNotFound, package.Package));
                }

                // Configured extensions make up our 'target' packages.
                listTargetIds.Add(packageIdentity.Id);

                // Search the graph of all the package dependencies to get the full set of available packages.
                await GetPackageDependencies(packageIdentity, sourceCacheContext, nugetLogger, repositories, availablePackages, cancelToken).ConfigureAwait(false);
            }

            // Create a package resolver context (this is used to help figure out which actual package versions to install).
            var resolverContext = new PackageResolverContext(
                   DependencyBehavior.Lowest,
                   listTargetIds,
                   Enumerable.Empty<string>(),
                   Enumerable.Empty<PackageReference>(),
                   Enumerable.Empty<PackageIdentity>(),
                   availablePackages,
                   sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
                   nugetLogger);

            var resolver = new PackageResolver();

            // Work out the actual set of packages to install.
            var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                                            .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));

            var packagePathResolver = new PackagePathResolver(packageDirectory, true);
            var packageExtractionContext = new PackageExtractionContext(
                PackageSaveMode.Defaultv3,
                XmlDocFileSaveMode.Skip,
                ClientPolicyContext.GetClientPolicy(sourceSettings.NuGetSettings, nugetLogger),
                nugetLogger);

            var frameworkReducer = new FrameworkReducer();

            var packageEntries = new List<PackageMetadata>();

            foreach (var package in packagesToInstall)
            {
                PackageReaderBase packageReader;
                var installedPath = packagePathResolver.GetInstalledPath(package);

                if (installedPath is null)
                {
                    var downloadResource = await package.Source.GetResourceAsync<DownloadResource>(cancelToken).ConfigureAwait(false);

                    // Download the package (might come from the shared package cache).
                    var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                        package,
                        new PackageDownloadContext(sourceCacheContext),
                        SettingsUtility.GetGlobalPackagesFolder(sourceSettings.NuGetSettings),
                        nugetLogger,
                        cancelToken).ConfigureAwait(false);

                    // Extract the package into the target directory.
                    await PackageExtractor.ExtractPackageAsync(
                        downloadResult.PackageSource,
                        downloadResult.PackageStream,
                        packagePathResolver,
                        packageExtractionContext,
                        cancelToken).ConfigureAwait(false);

                    packageReader = downloadResult.PackageReader;

                    // Get the path again.
                    installedPath = packagePathResolver.GetInstalledPath(package);
                }
                else
                {
                    #pragma warning disable CA2000 // Dispose objects before losing scope - we are doing this, in the finally block below.
                    // Analyzer isn't picking it up.
                    packageReader = new PackageFolderReader(installedPath);
                    #pragma warning restore CA2000
                }

                try
                {
                    var libItems = hostContext.GetFrameworkFiles(await packageReader.GetLibItemsAsync(cancelToken).ConfigureAwait(false));

                    string? entryPoint = null;

                    // Only consider as an entry point if the package contains the autostep tag.
                    if (entryPointPackageTag == null || packageReader.NuspecReader.GetTags().Contains(entryPointPackageTag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        entryPoint = libItems.FirstOrDefault(f => Path.GetFileName(f) == package.Id + ".dll");
                    }

                    // Create a package entry from the installed package.
                    var packageEntry = new PackageMetadata(
                        package.Id,
                        package.Version.ToNormalizedString(),
                        installedPath,
                        entryPoint,
                        libItems,
                        listTargetIds.Contains(package.Id),
                        package.Dependencies);

                    packageEntries.Add(packageEntry);
                }
                finally
                {
                    packageReader.Dispose();
                }
            }

            return new ExtensionPackages(packageDirectory, packageEntries);
        }

        /// <summary>
        /// Retrieves the actual package identity to install for an extension, given the configuration (based on requested version specifier, pre-release, etc).
        /// </summary>
        private async Task<PackageIdentity?> GetPackageIdentity(ExtensionConfiguration extConfig, SourceCacheContext cache, IEnumerable<SourceRepository> repositories, CancellationToken cancelToken)
        {
            // Go through each repository.
            // If a repository contains only pre-release packages (e.g. AutoStep CI), and the configuration doesn't permit pre-release versions,
            // the search will look at other ones (e.g. NuGet).
            foreach (var sourceRepository in repositories)
            {
                var findPackageResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);

                var allVersions = (await findPackageResource.GetAllVersionsAsync(extConfig.Package, cache, nugetLogger, cancelToken).ConfigureAwait(false)).ToList();

                NuGetVersion selected;

                // Have we specified a version range?
                if (extConfig.Version != null)
                {
                    if (!VersionRange.TryParse(extConfig.Version, out var range))
                    {
                        throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.NuGetPackagesLoader_BadVersionRange, extConfig.Package));
                    }

                    // Find the best package version match for the range.
                    // Consider pre-release versions, but only if the extension is configured to use them.
                    var bestVersion = range.FindBestMatch(allVersions.Where(v => extConfig.PreRelease || !v.IsPrerelease));

                    selected = bestVersion;
                }
                else
                {
                    // No version; choose the latest, allow pre-release if configured.
                    selected = allVersions.LastOrDefault(v => v.IsPrerelease == extConfig.PreRelease);
                }

                if (selected is object)
                {
                    return new PackageIdentity(extConfig.Package, selected);
                }
            }

            return null;
        }

        /// <summary>
        /// Searches the package dependency graph for the chain of all packages to install.
        /// </summary>
        private async Task GetPackageDependencies(
                PackageIdentity package,
                SourceCacheContext cacheContext,
                NuGet.Common.ILogger logger,
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
                    dependencyInfo.Dependencies.Where(dep => hostContext.DependencySuppliedByHost(dep)),
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
