using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    /// <summary>
    /// This class is liberally inspired by the post from Martin Bjorkstrom (https://martinbjorkstrom.com/posts/2018-09-19-revisiting-nuget-client-libraries), 
    /// and associated gist (https://gist.github.com/mholo65/ad5776c36559410f45d5dcd0181a5c64).
    /// </summary>
    internal class NugetExtensionLoader : BaseExtensionLoader
    {
        private readonly NuGet.Common.ILogger nugetLogger;
        private readonly string packageDirectory;
        private readonly ExtensionSourceSettings sourceSettings;
        private readonly DependencyContext hostDependencyContext;

        private readonly PackageSource NuGetSource = new PackageSource("https://api.nuget.org/v3/index.json", "nuget");

        public NugetExtensionLoader(string extensionsFolder, string frameworkName, ExtensionSourceSettings sourceSettings, DependencyContext hostDependencyContext, ILogger logger)
            : base(frameworkName)
        {
            nugetLogger = new NuGetLogger(logger);

            packageDirectory = extensionsFolder;
            this.sourceSettings = sourceSettings;
            this.hostDependencyContext = hostDependencyContext;
        }

        public async Task<ExtensionPackages> ResolveExtensionPackagesAsync(IConfiguration projConfig, CancellationToken cancelToken)
        {
            var extensions = projConfig.GetExtensionConfiguration();

            var sourceRepositoryProvider = new SourceRepositoryProvider(sourceSettings.SourceProvider, Repository.Provider.GetCoreV3());

            var repositories = sourceRepositoryProvider.GetRepositories();

            using var sourceCacheContext = new SourceCacheContext();

            var availablePackages = new HashSet<SourcePackageDependencyInfo>(PackageIdentityComparer.Default);
            var listTargetIds = new List<string>();

            foreach (var package in extensions)
            {
                // Determine the correct version.
                var packageIdentity = await GetPackageIdentity(package, sourceCacheContext, repositories, cancelToken);

                if (packageIdentity is null)
                {
                    throw new ProjectConfigurationException($"Could not locate extension package {package.Package}");
                }

                listTargetIds.Add(packageIdentity.Id);

                await GetPackageDependencies(packageIdentity, sourceCacheContext, nugetLogger, repositories, availablePackages, cancelToken);
            }

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
            var packagesToInstall = resolver.Resolve(resolverContext, CancellationToken.None)
                .Select(p => availablePackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));

            var packagePathResolver = new PackagePathResolver(packageDirectory, true);
            var packageExtractionContext = new PackageExtractionContext(
                PackageSaveMode.Defaultv3,
                XmlDocFileSaveMode.Skip,
                ClientPolicyContext.GetClientPolicy(sourceSettings.NuGetSettings, nugetLogger),
                nugetLogger);

            var frameworkReducer = new FrameworkReducer();

            var packageEntries = new List<PackageEntryWithDependencyInfo>();

            if (Directory.Exists(packageDirectory))
            {
                // Purge all the extension directories in the main one. We're going to get them from scratch anyway,
                // and this will make sure we clear out old package folders.
                foreach (var packageDir in Directory.GetDirectories(packageDirectory))
                {
                    Directory.Delete(packageDir, true);
                }
            }

            foreach (var package in packagesToInstall)
            {
                PackageReaderBase packageReader;
                var installedPath = packagePathResolver.GetInstalledPath(package);

                var downloadResource = await package.Source.GetResourceAsync<DownloadResource>(cancelToken);
                var downloadResult = await downloadResource.GetDownloadResourceResultAsync(
                    package,
                    new PackageDownloadContext(sourceCacheContext),
                    SettingsUtility.GetGlobalPackagesFolder(sourceSettings.NuGetSettings),
                    nugetLogger,
                    cancelToken);

                await PackageExtractor.ExtractPackageAsync(
                    downloadResult.PackageSource,
                    downloadResult.PackageStream,
                    packagePathResolver,
                    packageExtractionContext,
                    cancelToken);

                packageReader = downloadResult.PackageReader;

                // Get it again.
                installedPath = packagePathResolver.GetInstalledPath(package);

                var libItems = GetFrameworkFiles(await packageReader.GetLibItemsAsync(cancelToken));

                // Define the entry point (DLL with the same name as the package).
                var entryPoint = libItems.FirstOrDefault(f => Path.GetFileName(f) == package.Id + ".dll");

                var packageEntry = new PackageEntryWithDependencyInfo(
                    package,
                    installedPath,
                    entryPoint,
                    libItems,
                    GetFrameworkFiles(await packageReader.GetContentItemsAsync(cancelToken))
                );

                packageEntries.Add(packageEntry);
            }

            return new ExtensionPackages(packageDirectory, packageEntries);
        }

        private async Task<PackageIdentity> GetPackageIdentity(ExtensionConfiguration extConfig, SourceCacheContext cache, IEnumerable<SourceRepository> repositories, CancellationToken cancelToken)
        {
            foreach (var sourceRepository in repositories)
            {
                var findPackageResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();

                var allVersions = (await findPackageResource.GetAllVersionsAsync(extConfig.Package, cache, nugetLogger, cancelToken)).ToList();

                NuGetVersion selected;

                if (extConfig.Version != null)
                {
                    if (!VersionRange.TryParse(extConfig.Version, out var range))
                    {
                        throw new ProjectConfigurationException($"Invalid extension version range specified for the {extConfig.Package} extension.");
                    }

                    // Find the best package version match for the range. 
                    // Consider pre-release versions, but only if the extension is configured to use them.
                    var bestVersion = range.FindBestMatch(allVersions.Where(v => extConfig.PreRelease || !v.IsPrerelease));

                    selected = bestVersion;
                }
                else
                {
                    // Todo, use the pre-release setting.
                    selected = allVersions.LastOrDefault(v => v.IsPrerelease == extConfig.PreRelease);
                }

                if (selected is object)
                {
                    return new PackageIdentity(extConfig.Package, selected);
                }
            }

            return null;
        }

        private async Task GetPackageDependencies(PackageIdentity package,
                SourceCacheContext cacheContext,
                NuGet.Common.ILogger logger,
                IEnumerable<SourceRepository> repositories,
                ISet<SourcePackageDependencyInfo> availablePackages,
                CancellationToken cancelToken)
        {
            if (availablePackages.Contains(package))
            {
                return;
            }

            foreach (var sourceRepository in repositories)
            {
                var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();
                var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package, TargetFramework, cacheContext, logger, CancellationToken.None);

                if (dependencyInfo == null) continue;

                // Filter the dependency info.
                var actualSourceDep = new SourcePackageDependencyInfo(
                    dependencyInfo.Id,
                    dependencyInfo.Version,
                    dependencyInfo.Dependencies.Where(dep => hostDependencyContext.RuntimeLibraries.All(r => !LibrarySuppliedByHost(r, dep))),
                    dependencyInfo.Listed,
                    dependencyInfo.Source);

                availablePackages.Add(actualSourceDep);

                foreach (var dependency in actualSourceDep.Dependencies)
                {
                    await GetPackageDependencies(
                        new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion),
                        cacheContext, logger, repositories, availablePackages, cancelToken);
                }

                break;
            }
        }

        private bool LibrarySuppliedByHost(RuntimeLibrary r, PackageDependency dep)
        {
            return r.Name == dep.Id && dep.VersionRange.Satisfies(NuGetVersion.Parse(r.Version));
        }
    }
}
