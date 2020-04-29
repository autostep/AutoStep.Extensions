using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;

namespace AutoStep.Extensions.NuGetExtensions
{
    /// <summary>
    /// Represents an installable set of nuget packages.
    /// </summary>
    internal class NugetInstallablePackageSet : IInstallablePackageSet
    {
        private readonly ISourceSettings settings;
        private readonly IHostContext hostContext;
        private readonly IEnumerable<SourcePackageDependencyInfo> packagesToInstall;
        private readonly IReadOnlyList<string> targetIds;
        private readonly bool noCache;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NugetInstallablePackageSet"/> class.
        /// </summary>
        /// <param name="settings">The nuget settings.</param>
        /// <param name="hostContext">The host context.</param>
        /// <param name="packagesToInstall">The set of all packages to install.</param>
        /// <param name="targetIds">The set of targeted packages (i.e. extension packages).</param>
        /// <param name="noCache">If true, do not use the NuGet cache.</param>
        /// <param name="logger">A logger.</param>
        public NugetInstallablePackageSet(
            ISourceSettings settings,
            IHostContext hostContext,
            IEnumerable<SourcePackageDependencyInfo> packagesToInstall,
            IReadOnlyList<string> targetIds,
            bool noCache,
            ILogger logger)
        {
            this.settings = settings;
            this.hostContext = hostContext;
            this.packagesToInstall = packagesToInstall;
            this.targetIds = targetIds;
            this.noCache = noCache;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public IEnumerable<string> PackageIds => packagesToInstall.Select(x => x.Id);

        /// <inheritdoc/>
        public bool IsValid => true;

        /// <inheritdoc/>
        public Exception? Exception => null;

        /// <inheritdoc/>
        public async ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            if (!packagesToInstall.Any())
            {
                return InstalledExtensionPackages.Empty;
            }

            using var cacheContext = new SourceCacheContext();

            if (noCache)
            {
                cacheContext.MaxAge = DateTimeOffset.UtcNow;
            }

            var packagePathResolver = new PackagePathResolver(hostContext.ExtensionsDirectory, true);
            var packageExtractionContext = new PackageExtractionContext(
                PackageSaveMode.Defaultv3,
                XmlDocFileSaveMode.Skip,
                ClientPolicyContext.GetClientPolicy(settings.NuGetSettings, logger),
                logger);

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
                        new PackageDownloadContext(cacheContext),
                        SettingsUtility.GetGlobalPackagesFolder(settings.NuGetSettings),
                        logger,
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
                    if (hostContext.EntryPointPackageTag == null || packageReader.NuspecReader.GetTags().Contains(hostContext.EntryPointPackageTag, StringComparison.InvariantCultureIgnoreCase))
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
                        targetIds!.Contains(package.Id),
                        package.Dependencies.Select(x => x.Id));

                    packageEntries.Add(packageEntry);
                }
                finally
                {
                    packageReader.Dispose();
                }
            }

            return new InstalledExtensionPackages(packageEntries);
        }
    }
}
