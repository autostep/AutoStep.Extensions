using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
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

            var depFilePath = Path.Combine(extensionsFolder, ExtensionDependencyFile);

            ExtensionPackages? resolvedPackages = null;

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

            var loadedSet = new ExtensionSet(projectConfig, resolvedPackages);

            loadedSet.Load(logFactory);

            return loadedSet;
        }

        private static DependencyContext? LoadCachedDependencyInfo(string depFilePath, ILogger logger)
        {
            try
            {
                using var dependencyContextLdr = new DependencyContextJsonReader();
                using var stream = File.OpenRead(depFilePath);

                return dependencyContextLdr.Read(stream);
            }
            catch(Exception ex)
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
