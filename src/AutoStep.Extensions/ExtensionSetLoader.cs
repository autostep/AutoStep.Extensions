using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;

namespace AutoStep.Extensions
{
    public static class ExtensionSetLoader
    {
        private const string DefaultFramework = ".NETCoreApp,Version=3.1";

        private const string AutoStepHiddenDirectory = ".autostep";
        private const string AutoStepExtensionDirectory = "extensions";
        private const string ExtensionDependencyFile = "extensions.deps.json";

        public static async Task<IExtensionSet> LoadExtensionsAsync(
            string rootDirectory,
            Assembly hostAssembly,
            ExtensionSourceSettings sourceSettings,
            ILoggerFactory logFactory,
            IConfiguration projectConfig,
            CancellationToken cancelToken)
        {
            var targetFramework = hostAssembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? DefaultFramework;

            var logger = logFactory.CreateLogger("extensions");

            var extensionsFolder = Path.GetFullPath(Path.Combine(AutoStepHiddenDirectory, AutoStepExtensionDirectory), rootDirectory);

            // Ensure that the extensions folder exists.
            var depFilePath = Path.Combine(extensionsFolder, ExtensionDependencyFile);

            ExtensionPackages? resolvedPackages = null;

            // Take a lock on the dependency file.
            using (var dependencyFileLock = await TakePathLock(depFilePath, cancelToken))
            {
                if (File.Exists(depFilePath))
                {
                    // Load the dependency context.
                    DependencyContext? cacheDepCtxt = LoadCachedDependencyInfo(depFilePath, logger);

                    if (cacheDepCtxt is object)
                    {
                        var cachedLoader = new CachedExtensionLoader(extensionsFolder, targetFramework, logger);

                        resolvedPackages = cachedLoader.ResolveExtensionPackages(projectConfig, cacheDepCtxt);
                    }
                }

                if (resolvedPackages is null)
                {
                    // Need the dependency context for the host assembly.
                    var depContext = LoadHostDependencyContext(hostAssembly);

                    var nugetLoader = new NugetExtensionLoader(extensionsFolder, targetFramework, sourceSettings, depContext, logger);

                    resolvedPackages = await nugetLoader.ResolveExtensionPackagesAsync(projectConfig, cancelToken).ConfigureAwait(false);

                    // Save the new set of packages.
                    SaveExtensionDependencyContext(projectConfig, depContext, resolvedPackages, depFilePath);
                }
            }

            var loadedSet = new ExtensionSet(projectConfig, resolvedPackages);

            loadedSet.Load(logFactory);

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

        private static DependencyContext? LoadCachedDependencyInfo(string depFilePath, ILogger logger)
        {
            try
            {
                using var dependencyContextLdr = new DependencyContextJsonReader();
                using var stream = File.OpenRead(depFilePath);

                return dependencyContextLdr.Read(stream);
            }
            catch (Exception ex)
            {
                // Corrupt file, IO error? Lets just assume we can't get the info.
                logger.LogWarning(ex, "Corrupt or unavailable extension cache file. Ignoring cache.");
            }

            return null;
        }

        private static void SaveExtensionDependencyContext(IConfiguration projectConfig, DependencyContext hostContext, ExtensionPackages packages, string outputPath)
        {
            var targetIds = projectConfig.GetExtensionConfiguration().Select(x => x.Package).ToList();

            var newDepContext = new DependencyContext(
                hostContext.Target,
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                packages.Packages.Select(p => new RuntimeLibrary(
                    targetIds.Contains(p.PackageId) ? ExtensionRuntimeLibraryType.RootPackage : ExtensionRuntimeLibraryType.Dependency,
                    p.PackageId,
                    p.PackageVersion,
                    null,
                    new[] {
                        new RuntimeAssetGroup(
                            hostContext.Target.Runtime,
                            p.LibFiles.Where(f => Path.GetExtension(f) == ".dll")
                            .Select(f => GetRuntimeFile(p, f)))
                    },
                    new List<RuntimeAssetGroup>(),
                    Enumerable.Empty<ResourceAssembly>(),
                    p.Dependencies.Select(d => packages.Packages.FirstOrDefault(p => p.PackageId == d.Id))
                                  .Where(d => d is object)
                                  .Select(d => new Dependency(d.PackageId, d.PackageVersion)),
                    true
                    )),
                Enumerable.Empty<RuntimeFallbacks>()
            );

            // Write the dependency files.
            var dependencyWriter = new DependencyContextWriter();

            using (var fileStream = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                dependencyWriter.Write(newDepContext, fileStream);
            }

        }

        private static RuntimeFile GetRuntimeFile(PackageEntry packageEntry, string file)
        {
            var fullPath = Path.GetFullPath(file, packageEntry.PackageFolder);

            var assemblyName = AssemblyName.GetAssemblyName(fullPath);

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);

            return new RuntimeFile(file, assemblyName.Version?.ToString(), fileVersionInfo.FileVersion);
        }

        private static DependencyContext LoadHostDependencyContext(Assembly hostAssembly)
        {
            return DependencyContext.Load(hostAssembly);
        }
    }
}
