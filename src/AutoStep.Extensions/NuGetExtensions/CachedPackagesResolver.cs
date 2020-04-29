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

namespace AutoStep.Extensions.NuGetExtensions
{
    /// <summary>
    /// A resolver for the set of cached packages already installed.
    /// </summary>
    internal class CachedPackagesResolver : IExtensionPackagesResolver
    {
        private readonly IHostContext hostContext;
        private readonly string dependencyJsonFile;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedPackagesResolver"/> class.
        /// </summary>
        /// <param name="hostContext">The host context.</param>
        /// <param name="dependencyJsonFile">The path to the dependency cache JSON file.</param>
        /// <param name="logger">A logger.</param>
        public CachedPackagesResolver(IHostContext hostContext, string dependencyJsonFile, ILogger logger)
        {
            this.hostContext = hostContext;
            this.dependencyJsonFile = dependencyJsonFile;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async ValueTask<IInstallablePackageSet> ResolvePackagesAsync(ExtensionResolveContext resolveContext, CancellationToken cancelToken)
        {
            logger.LogDebug(Messages.CachedPackagesResolver_CacheExistsVerifying);

            var knownCache = LoadCachedDependencyInfo();

            if (knownCache is null)
            {
                return new InvalidPackageSet();
            }

            var cacheValid = ValidateRootPackages(resolveContext, knownCache);

            if (cacheValid)
            {
                logger.LogDebug(Messages.CachedPackagesResolver_RootPackagesValid);

                // Now we need to validate the files and build the package set.
                var packageSet = await LoadPackageSet(knownCache, cancelToken).ConfigureAwait(false);

                if (packageSet is object)
                {
                    logger.LogDebug(Messages.CachedPackagesResolver_CacheValid);
                    return new AlreadyInstalledPackagesSet(new InstalledExtensionPackages(packageSet));
                }
                else
                {
                    logger.LogDebug(Messages.CachedPackagesResolver_NotAllFilesAvailable);
                }
            }
            else
            {
                logger.LogDebug(Messages.CachedPackagesResolver_RootPackagesNotValid);
            }

            // Return an invalid package set.
            return new InvalidPackageSet();
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

        /// <summary>
        /// Look for all runtime libraries that are declared as 'rootPackage' type.
        /// If the set of root packages have changed from the list of extensions, we can throw away
        /// the cache and retrieve again.
        ///
        /// If an explicit version is set in the extensions config, and that version is no longer met by
        /// the version in the cache, then we will also go again.
        ///
        /// If any 'additional packages' have been requested, and they are not also present in the cache, then don't use the cache.
        /// </summary>
        private bool ValidateRootPackages(ExtensionResolveContext resolveContext, DependencyContext knownCache)
        {
            var rootRuntimeLibraries = knownCache.RuntimeLibraries.Where(x => x.Type == ExtensionRuntimeLibraryTypes.RootPackage).ToDictionary(x => x.Name);
            var isValid = true;

            isValid = ProcessExtensionPackages(resolveContext, rootRuntimeLibraries) &&
                      ProcessAdditionalPackages(resolveContext, knownCache.RuntimeLibraries);

            foreach (var extraRootPackage in rootRuntimeLibraries)
            {
                logger.LogDebug(Messages.CachedPackagesResolver_DependencyCacheContainsUnrequiredPackage, extraRootPackage.Key);
                isValid = false;
            }

            return isValid;
        }

        private bool ProcessExtensionPackages(ExtensionResolveContext resolveContext, Dictionary<string, RuntimeLibrary> rootRuntimeLibraries)
        {
            var isValid = true;

            foreach (var extConfig in resolveContext.PackageExtensions)
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
                                    logger.LogDebug(Messages.CachedPackagesResolver_ConfiguredExtensionVersionNotCompatibleWithCache, extConfig.Package, extConfig.Version, lib.Version);
                                    isValid = false;
                                }
                            }
                            else
                            {
                                throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.CachedPackagesResolver_InvalidExtensionVersionRange, extConfig.Package));
                            }
                        }

                        if (extConfig.PreRelease == false && parsedCacheVersion.IsPrerelease)
                        {
                            // Pre-releases no longer allowed.
                            logger.LogDebug(Messages.CachedPackagesResolver_PreReleaseInCacheNotAllowed, extConfig.Package);
                            isValid = false;
                        }
                    }
                    else
                    {
                        logger.LogDebug(Messages.CachedPackagesResolver_BadVersionInDependencyCache, extConfig.Package);
                        isValid = false;
                    }
                }
                else
                {
                    logger.LogDebug(Messages.CachedPackagesResolver_NoEntryInDependencyCache, extConfig.Package);
                    isValid = false;
                }

                if (isValid)
                {
                    logger.LogDebug(Messages.CachedPackagesResolver_CachedExtensionInfoIsValid, extConfig.Package);
                }
            }

            return isValid;
        }

        private bool ProcessAdditionalPackages(ExtensionResolveContext resolveContext, IReadOnlyList<RuntimeLibrary> runtimeLibraries)
        {
            var isValid = true;

            foreach (var dependency in resolveContext.AdditionalPackagesRequired)
            {
                var foundLib = runtimeLibraries.FirstOrDefault(x => x.Name == dependency.Id);

                // Look for a runtime library.
                if (foundLib is object)
                {
                    // Ok, so we have a cached entry; is it the right version?
                    if (NuGetVersion.TryParse(foundLib.Version, out var parsedCacheVersion))
                    {
                        if (!dependency.VersionRange.Satisfies(parsedCacheVersion))
                        {
                            // Range not satisfied.
                            logger.LogDebug(Messages.CachedPackagesResolver_AdditionalDependencyVersionNotCompatibleWithCache, dependency.Id, dependency.VersionRange.ToString(), foundLib.Version);
                            isValid = false;
                        }
                    }
                    else
                    {
                        logger.LogDebug(Messages.CachedPackagesResolver_BadVersionInDependencyCache, dependency.Id);
                        isValid = false;
                    }
                }
                else
                {
                    logger.LogDebug(Messages.CachedPackagesResolver_NoEntryInDependencyCache, dependency.Id);
                    isValid = false;
                }

                if (isValid)
                {
                    logger.LogDebug(Messages.CachedPackagesResolver_CachedAdditionalDepIsValid, dependency.Id);
                }
            }

            return isValid;
        }

        private async Task<IReadOnlyList<PackageMetadata>?> LoadPackageSet(DependencyContext knownCache, CancellationToken cancelToken)
        {
            var loadedPackages = new List<PackageMetadata>();
            var isValid = true;
            var packagePathResolver = new PackagePathResolver(hostContext.ExtensionsDirectory, true);

            foreach (var runtimeLib in knownCache.RuntimeLibraries)
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
                            logger.LogDebug(Messages.CachedPackagesResolver_CannotFindLibraryFile, cachedFile, packageId);
                            isValid = false;
                        }
                    }

                    string? entryPoint = null;

                    // Only consider as an entry point if the package contains the autostep tag.
                    if (hostContext.EntryPointPackageTag is null || nugetFolderReader.NuspecReader.GetTags().Contains(hostContext.EntryPointPackageTag, StringComparison.InvariantCultureIgnoreCase))
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
                        runtimeLib.Dependencies.Select(d => d.Name)));
                }
                else
                {
                    logger.LogDebug(Messages.CachedPackagesResolver_PackageDirectoryDoesNotExist, runtimeLib.Path);
                    isValid = false;
                }
            }

            if (isValid)
            {
                return loadedPackages;
            }

            return null;
        }
    }
}
