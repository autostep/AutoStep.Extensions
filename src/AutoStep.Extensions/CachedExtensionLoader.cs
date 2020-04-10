using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace AutoStep.Extensions
{
    internal class CachedExtensionLoader : BaseExtensionLoader
    {
        private readonly string extensionsFolder;
        private readonly ILogger logger;

        public CachedExtensionLoader(string extensionsFolder, string frameworkName, ILogger logger)
            : base(frameworkName)
        {
            this.extensionsFolder = extensionsFolder;
            this.logger = logger;
        }

        public ExtensionPackages? ResolveExtensionPackages(IConfiguration projConfig, DependencyContext knownCache)
        {
            ExtensionPackages? resolvedPackages = null;

            var configuredExtensions = projConfig.GetExtensionConfiguration();

            logger.LogDebug("Extension dependency cache data exists; verifying.");

            var cacheValid = ValidateRootPackages(configuredExtensions, knownCache);

            if (cacheValid)
            {
                logger.LogDebug("Set of root packages is valid; verifying package files.");

                // Now we need to validate the files and build the package set.
                if (TryGetPackageSet(knownCache, out var packageSet))
                {
                    logger.LogDebug("Package files available. Cache valid, using it.");
                    resolvedPackages = new ExtensionPackages(extensionsFolder, packageSet);
                }
                else
                {
                    logger.LogDebug("Package files not all available; ignoring cache.");
                }
            }
            else
            {
                logger.LogDebug("Set of root packages not valid; ignoring cache.");
            }

            return resolvedPackages;
        }

        private bool TryGetPackageSet(DependencyContext depContext, out IReadOnlyList<PackageEntry> packageSet)
        {
            var loadedPackages = new List<PackageEntry>();
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
                    var nugetFolderReader = new PackageFolderReader(packageDir);

                    var libFiles = GetFrameworkFiles(nugetFolderReader.GetLibItems());
                    var contentFiles = GetFrameworkFiles(nugetFolderReader.GetContentItems());

                    // Verify that the libraries match the libs specified in the cache.
                    var cachedAssemblyPaths = GetRuntimeLibraryLibPaths(runtimeLib) ?? Enumerable.Empty<string>();

                    foreach (var cachedFile in cachedAssemblyPaths)
                    {
                        if (!libFiles.Contains(cachedFile))
                        {
                            // Cached file not present in folder, corrupt package folder most likely.
                            logger.LogDebug("Cannot find library file {0} for cached package {1}.", cachedFile);
                            isValid = false;
                        }
                    }

                    var entryPoint = libFiles.FirstOrDefault(f => Path.GetFileName(f) == runtimeLib.Name + ".dll");

                    loadedPackages.Add(new PackageEntry(runtimeLib.Name, runtimeLib.Version, packageDir, entryPoint,
                                                        libFiles, contentFiles, runtimeLib.Dependencies.Select(d => new PackageDependency(d.Name))));
                }
                else
                {
                    logger.LogDebug("Install directory for cached package {0} does not exist.", runtimeLib.Path);
                    isValid = false;
                }
            }

            packageSet = loadedPackages;
            return isValid;
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
            var rootRuntimeLibraries = depContext.RuntimeLibraries.Where(x => x.Type == ExtensionRuntimeLibraryType.RootPackage).ToDictionary(x => x.Name);
            var isValid = true;

            foreach (var extConfig in extensions)
            {
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
                                    logger.LogDebug("Version range specified in the {0} extension configuration, {1}, is not a match for the cached version, {2}.", extConfig.Package, extConfig.Version, lib.Version);
                                    isValid = false;
                                }
                            }
                            else
                            {
                                throw new ProjectConfigurationException($"Invalid extension version range specified for the {extConfig.Package} extension.");
                            }
                        }

                        if (extConfig.PreRelease == false && parsedCacheVersion.IsPrerelease)
                        {
                            // Pre-releases no longer allowed.
                            logger.LogDebug("The cached extension version for {0} is a pre-release, but the extension configuration does not allow pre-releases.", extConfig.Package);
                            isValid = false;
                        }
                    }
                    else
                    {
                        logger.LogDebug("Bad version in dependency cache for {0}", extConfig.Package);
                        isValid = false;
                    }
                }
                else
                {
                    logger.LogDebug("No entry in dependency cache for {0}", extConfig.Package);
                    isValid = false;
                }

                if (isValid)
                {
                    logger.LogDebug("Cached extension dependency info for {0} is valid.", extConfig.Package);
                }
            }

            foreach (var extraRootPackage in rootRuntimeLibraries)
            {
                logger.LogDebug("Extension dependency cache contains extension package {0} that has not been requested by configuration.", extraRootPackage.Key);
                isValid = false;
            }

            return isValid;
        }
    }
}
