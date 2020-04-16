using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Validates and loads the packages defined in the cached set of packages.
    /// </summary>
    internal class CachedPackagesLoader
    {
        private readonly string extensionsFolder;
        private readonly IHostContext hostContext;
        private readonly string? entryPointPackageTag;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedPackagesLoader"/> class.
        /// </summary>
        /// <param name="extensionsFolder">The extensions folder.</param>
        /// <param name="hostContext">The host context.</param>
        /// <param name="entryPointPackageTag">An optional tag name to filter loaded packages considered as possible 'entry points' for extensions.</param>
        /// <param name="logger">A logger.</param>
        public CachedPackagesLoader(string extensionsFolder, IHostContext hostContext, string? entryPointPackageTag, ILogger logger)
        {
            this.extensionsFolder = extensionsFolder;
            this.hostContext = hostContext;
            this.entryPointPackageTag = entryPointPackageTag;
            this.logger = logger;
        }

        /// <summary>
        /// Validate and load a set of cached package files using a set of cached package metadata.
        /// </summary>
        /// <param name="configuredExtensions">The set of configured extensions.</param>
        /// <param name="knownCache">The known cached metadata.</param>
        /// <param name="cancelToken">A cancellation token.</param>
        /// <returns>The set of loaded packages, or null if the set was not valid.</returns>
        public async Task<ExtensionPackages?> LoadExtensionPackagesAsync(IEnumerable<ExtensionConfiguration> configuredExtensions, DependencyContext knownCache, CancellationToken cancelToken)
        {
            ExtensionPackages? resolvedPackages = null;

            logger.LogDebug(Messages.CachedPackagesLoader_CacheExistsVerifying);

            var cacheValid = ValidateRootPackages(configuredExtensions, knownCache);

            if (cacheValid)
            {
                logger.LogDebug(Messages.CachedPackagesLoader_RootPackagesValid);

                // Now we need to validate the files and build the package set.
                var packageSet = await LoadPackageSet(knownCache, cancelToken).ConfigureAwait(false);

                if (packageSet is object)
                {
                    logger.LogDebug(Messages.CachedPackagesLoader_CacheValid);
                    resolvedPackages = new ExtensionPackages(extensionsFolder, packageSet);
                }
                else
                {
                    logger.LogDebug(Messages.CachedPackagesLoader_NotAllFilesAvailable);
                }
            }
            else
            {
                logger.LogDebug(Messages.CachedPackagesLoader_RootPackagesNotValid);
            }

            return resolvedPackages;
        }

        private async Task<IReadOnlyList<PackageMetadata>?> LoadPackageSet(DependencyContext depContext, CancellationToken cancelToken)
        {
            var loadedPackages = new List<PackageMetadata>();
            var isValid = true;
            var packagePathResolver = new PackagePathResolver(extensionsFolder, true);

            foreach (var runtimeLib in depContext.RuntimeLibraries)
            {
                var packageId = new PackageIdentity(runtimeLib.Name, NuGetVersion.Parse(runtimeLib.Version));

                // First off, does the package folder exist?
                var packageDir = packagePathResolver.GetInstalledPath(packageId);

                if (packageDir is object)
                {
                    // We have a package directory; now we need to validate the library files exist.
                    using var nugetFolderReader = new PackageFolderReader(packageDir);

                    var libFiles = hostContext.GetFrameworkFiles(await nugetFolderReader.GetLibItemsAsync(cancelToken).ConfigureAwait(false));

                    // Verify that the libraries match the libs specified in the cache.
                    var cachedAssemblyPaths = hostContext.GetRuntimeLibraryLibPaths(runtimeLib) ?? Enumerable.Empty<string>();

                    foreach (var cachedFile in cachedAssemblyPaths)
                    {
                        if (!libFiles.Contains(cachedFile))
                        {
                            // Cached file not present in folder, corrupt package folder most likely.
                            logger.LogDebug(Messages.CachedPackagesLoader_CannotFindLibraryFile, cachedFile, packageId);
                            isValid = false;
                        }
                    }

                    string? entryPoint = null;

                    // Only consider as an entry point if the package contains the autostep tag.
                    if (entryPointPackageTag is null || nugetFolderReader.NuspecReader.GetTags().Contains(entryPointPackageTag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        entryPoint = libFiles.FirstOrDefault(f => Path.GetFileName(f) == runtimeLib.Name + ".dll");
                    }

                    loadedPackages.Add(new PackageMetadata(
                        runtimeLib.Name,
                        runtimeLib.Version,
                        packageDir,
                        entryPoint,
                        libFiles,
                        runtimeLib.Type == ExtensionRuntimeLibraryTypes.RootPackage,
                        runtimeLib.Dependencies.Select(d => new PackageDependency(d.Name))));
                }
                else
                {
                    logger.LogDebug(Messages.CachedPackagesLoader_PackageDirectoryDoesNotExist, runtimeLib.Path);
                    isValid = false;
                }
            }

            if (isValid)
            {
                return loadedPackages;
            }

            return null;
        }

        /// <summary>
        /// Look for all runtime libraries that are declared as 'rootPackage' type.
        /// If the set of root packages have changed from the list of extensions, we can throw away
        /// the cache and retrieve again.
        ///
        /// If an explicit version is set in the extensions config, and that version is no longer met by
        /// the version in the cache, then we will also go again.
        /// </summary>
        private bool ValidateRootPackages(IEnumerable<ExtensionConfiguration> extensions, DependencyContext depContext)
        {
            var rootRuntimeLibraries = depContext.RuntimeLibraries.Where(x => x.Type == ExtensionRuntimeLibraryTypes.RootPackage).ToDictionary(x => x.Name);
            var isValid = true;

            foreach (var extConfig in extensions)
            {
                if (string.IsNullOrWhiteSpace(extConfig.Package))
                {
                    continue;
                }

                // Look for a runtime library.
                if (rootRuntimeLibraries.TryGetValue(extConfig.Package, out var lib))
                {
                    // Remove the item from the dictionary (to give us our 'extras' list at the end of the loop).
                    rootRuntimeLibraries.Remove(extConfig.Package);

                    // Ok, so we have a cached entry; is it the right version?
                    if (NuGetVersion.TryParse(lib.Version, out var parsedCacheVersion))
                    {
                        if (extConfig.Version is object)
                        {
                            if (VersionRange.TryParse(extConfig.Version, out var range))
                            {
                                if (!range.Satisfies(parsedCacheVersion))
                                {
                                    // Range not satisfied.
                                    logger.LogDebug(Messages.CachedPackagesLoader_ConfiguredVersionNotCompatibleWithCache, extConfig.Package, extConfig.Version, lib.Version);
                                    isValid = false;
                                }
                            }
                            else
                            {
                                throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.CachedPackagesLoader_InvalidExtensionVersionRange, extConfig.Package));
                            }
                        }

                        if (extConfig.PreRelease == false && parsedCacheVersion.IsPrerelease)
                        {
                            // Pre-releases no longer allowed.
                            logger.LogDebug(Messages.CachedPackagesLoader_PreReleaseInCacheNotAllowed, extConfig.Package);
                            isValid = false;
                        }
                    }
                    else
                    {
                        logger.LogDebug(Messages.CachedPackagesLoader_BadVersionInDependencyCache, extConfig.Package);
                        isValid = false;
                    }
                }
                else
                {
                    logger.LogDebug(Messages.CachedPackagesLoader_NoEntryInDependencyCache, extConfig.Package);
                    isValid = false;
                }

                if (isValid)
                {
                    logger.LogDebug(Messages.CachedPackagesLoader_CachedExtensionInfoIsValid, extConfig.Package);
                }
            }

            foreach (var extraRootPackage in rootRuntimeLibraries)
            {
                logger.LogDebug(Messages.CachedPackagesLoader_DependencyCacheContainsUnrequiredPackage, extraRootPackage.Key);
                isValid = false;
            }

            return isValid;
        }
    }
}
