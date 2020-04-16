using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.Abstractions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Entry point for loading extensions.
    /// </summary>
    public class ExtensionSetLoader
    {
        private const string ExtensionDependencyFile = "extensions.deps.json";

        private readonly string dependencyJsonFile;
        private readonly string extensionsDirectory;
        private readonly ILoggerFactory loggerFactory;
        private readonly IHostContext hostContext;
        private readonly ILogger<ExtensionSetLoader> logger;
        private readonly CachedPackagesLoader cachedLoader;
        private readonly NuGetPackagesLoader nugetLoader;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionSetLoader"/> class.
        /// </summary>
        /// <param name="extensionsDirectory">The directory that should contain all extension-related packages and files.</param>
        /// <param name="loggerFactory">A logger factory to which the extension load process will write information.</param>
        /// <param name="extensionPackageTag">An optional package tag to filter the set of packages that will be considered as 'entry points', and therefore implicitly loaded.</param>
        public ExtensionSetLoader(string extensionsDirectory, ILoggerFactory loggerFactory, string? extensionPackageTag)
        {
            if (!Path.IsPathFullyQualified(extensionsDirectory))
            {
                throw new ArgumentException(Messages.ExtensionSetLoader_ExtensionDirectoryMustBeFullyQualified);
            }

            this.extensionsDirectory = extensionsDirectory;
            this.loggerFactory = loggerFactory;

            var hostAssembly = typeof(ExtensionSetLoader).Assembly;

            // Need the dependency context for the host assembly, create a host context from that.
            hostContext = new HostContext(hostAssembly);

            logger = loggerFactory.CreateLogger<ExtensionSetLoader>();

            // Ensure that the extensions folder exists.
            dependencyJsonFile = Path.Combine(extensionsDirectory, ExtensionDependencyFile);

            cachedLoader = new CachedPackagesLoader(extensionsDirectory, hostContext, extensionPackageTag, logger);
            nugetLoader = new NuGetPackagesLoader(extensionsDirectory, hostContext, extensionPackageTag, logger);
        }

        /// <summary>
        /// Load a set of extensions.
        /// </summary>
        /// <typeparam name="TExtensionEntryPoint">
        /// The load process will look for an 'entry point' in each loaded extension that is assignable to this type.
        /// Typically this is an interface, or possibly an abstract class.</typeparam>
        /// <param name="sourceSettings">The source settings, dictating where packages come from.</param>
        /// <param name="configuredExtensions">A set of extension configuration, specifying which extensions to install.</param>
        /// <param name="noCache">If set to true, then any existing caches will be ignored, and the set of packages will be downloaded directly.</param>
        /// <param name="cancelToken">A cancellation token to abort the process.</param>
        /// <returns>An awaitable task, containing the set of loaded extensions.</returns>
        public async Task<ILoadedExtensions<TExtensionEntryPoint>> LoadExtensionsAsync<TExtensionEntryPoint>(
            ISourceSettings sourceSettings,
            IEnumerable<ExtensionConfiguration> configuredExtensions,
            bool noCache,
            CancellationToken cancelToken)
            where TExtensionEntryPoint : IDisposable
        {
            if (sourceSettings is null)
            {
                throw new ArgumentNullException(nameof(sourceSettings));
            }

            if (configuredExtensions is null)
            {
                throw new ArgumentNullException(nameof(configuredExtensions));
            }

            ExtensionPackages? resolvedPackages = null;

            // Ensure that the extensions directory exists.
            Directory.CreateDirectory(extensionsDirectory);

            // Take a lock on the dependency file.
            using (var dependencyFileLock = await TakePathLock(dependencyJsonFile, cancelToken))
            {
                if (!noCache && File.Exists(dependencyJsonFile))
                {
                    // Load the dependency context.
                    DependencyContext? cacheDepCtxt = LoadCachedDependencyInfo();

                    if (cacheDepCtxt is object)
                    {
                        resolvedPackages = await cachedLoader.LoadExtensionPackagesAsync(configuredExtensions, cacheDepCtxt, cancelToken).ConfigureAwait(false);
                    }
                }

                if (resolvedPackages is null)
                {
                    resolvedPackages = await nugetLoader.LoadExtensionPackagesAsync(sourceSettings, configuredExtensions, false, cancelToken).ConfigureAwait(false);

                    // Save the new set of packages.
                    SaveExtensionDependencyContext(configuredExtensions, resolvedPackages);
                }
            }

            // Loads the extension entry points.
            var loadedSet = new LoadedExtensions<TExtensionEntryPoint>(resolvedPackages);

            loadedSet.LoadEntryPoints(loggerFactory);

            return loadedSet;
        }

        private static async ValueTask<IDisposable> TakePathLock(string depFilePath, CancellationToken cancelToken)
        {
            FileStream? fileStream = null;

            // Hash the dependency file path and use that for the lock file.
            // Doing this rather than taking a lock on the dependency file itself, because we
            // want to allow a read-only dependency file.
            var lockPath = Path.Combine(Path.GetTempPath(), HashPath(depFilePath));

            while (fileStream is null)
            {
                try
                {
                    fileStream = File.Open(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                    cancelToken.ThrowIfCancellationRequested();
                }
                catch (IOException)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    // In use (or cannot use); go round again. Give it a few milliseconds.
                    await Task.Delay(10).ConfigureAwait(false);
                }
            }

            return fileStream;
        }

        /// <summary>
        /// Creates a hash of an absolute path, that we can then use in another file name.
        /// </summary>
        private static string HashPath(string path)
        {
            const string pathPrefix = "aslock_";

            // First, hash it (mostly to shorten).
            using var hasher = SHA256.Create();

            var hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(path.ToUpperInvariant()));

            // Just use hex encoding.
            var createdString = string.Create(pathPrefix.Length + (hash.Length * 2), hash, (span, source) =>
            {
                var culture = CultureInfo.InvariantCulture;

                // Copy the constant.
                var prefixSpan = pathPrefix.AsSpan();

                prefixSpan.CopyTo(span);
                span = span.Slice(prefixSpan.Length);

                for (int idx = 0; idx < source.Length; idx++)
                {
                    var hex = source[idx].ToString("x2", culture);
                    span[0] = hex[0];
                    span[1] = hex[1];

                    // Move forward in the span.
                    span = span.Slice(2);
                }
            });

            return createdString;
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

        private void SaveExtensionDependencyContext(IEnumerable<ExtensionConfiguration> configuredExtensions, ExtensionPackages packages)
        {
            var targetIds = configuredExtensions.Select(x => x.Package!).ToList();

            var newDepContext = new DependencyContext(
                hostContext.Target,
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                packages.Packages.Select(p => RuntimeLibraryFromPackage(targetIds, p, packages.Packages)),
                Enumerable.Empty<RuntimeFallbacks>());

            // Write the dependency file.
            var dependencyWriter = new DependencyContextWriter();

            using (var fileStream = File.Open(dependencyJsonFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                dependencyWriter.Write(newDepContext, fileStream);
            }
        }

        private RuntimeLibrary RuntimeLibraryFromPackage(IReadOnlyList<string> extensionIds, PackageMetadata package, IReadOnlyList<PackageMetadata> allPackages)
        {
            var runtimeAssetGroup = new RuntimeAssetGroup(hostContext.Target.Runtime, package.LibFiles.Where(f => Path.GetExtension(f) == ".dll")
                                                                                         .Select(f => GetRuntimeFile(package, f)));

            return new RuntimeLibrary(
                      extensionIds.Contains(package.PackageId) ? ExtensionRuntimeLibraryTypes.RootPackage : ExtensionRuntimeLibraryTypes.Dependency,
                      package.PackageId,
                      package.PackageVersion,
                      null,
                      new[] { runtimeAssetGroup },
                      new List<RuntimeAssetGroup>(),
                      Enumerable.Empty<ResourceAssembly>(),
                      package.Dependencies.Select(d => allPackages.FirstOrDefault(p => p.PackageId == d.Id))
                                    .Where(d => d is object)
                                    .Select(d => new Dependency(d.PackageId, d.PackageVersion)),
                      true);
        }

        private static RuntimeFile GetRuntimeFile(PackageMetadata packageEntry, string file)
        {
            var fullPath = Path.GetFullPath(file, packageEntry.PackageFolder);

            var assemblyName = AssemblyName.GetAssemblyName(fullPath);

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);

            return new RuntimeFile(file, assemblyName.Version?.ToString(), fileVersionInfo.FileVersion);
        }
    }
}
