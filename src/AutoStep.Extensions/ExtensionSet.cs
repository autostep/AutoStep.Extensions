using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoStep.Extensions
{
    internal sealed class ExtensionSet : IExtensionSet
    {
        private readonly List<IExtensionEntryPoint> extensions = new List<IExtensionEntryPoint>();
        private readonly IReadOnlyList<string> requiredPackages;

        private ExtLoadContext loadContext;
        private readonly WeakReference weakContextReference;
        private readonly ExtensionPackages extPackages;

        private bool isDisposed;

        public ExtensionSet(IConfiguration projectConfig, ExtensionPackages packages)
        {
            loadContext = new ExtLoadContext(packages);
            weakContextReference = new WeakReference(loadContext);
            isDisposed = false;

            // Get the set of required extensions.
            var extensions = projectConfig.GetExtensionConfiguration();

            this.requiredPackages = extensions.Select(e => e.Package).ToList();
            this.extPackages = packages;
            this.ExtensionsRootDir = packages.ExtensionsRootDir;
        }

        public string ExtensionsRootDir { get; private set; }

        public IEnumerable<IPackageMetadata> LoadedPackages => extPackages.Packages;

        /// <summary>
        /// Called prior to execution.
        /// </summary>
        /// <param name="builder">The services builder.</param>
        /// <param name="configuration">The test run configuration.</param>
        public void ConfigureExtensionServices(IConfiguration configuration, IServicesBuilder builder)
        {
            foreach (var ext in extensions)
            {
                ext.ConfigureExecutionServices(configuration, builder);
            }
        }

        public void AttachToProject(IConfiguration projectConfiguration, Project project)
        {
            foreach (var ext in extensions)
            {
                ext.AttachToProject(projectConfiguration, project);
            }
        }

        public void ExtendExecution(IConfiguration projectConfiguration, TestRun testRun)
        {
            foreach (var ext in extensions)
            {
                ext.ExtendExecution(projectConfiguration, testRun);
            }
        }

        public void Load(ILoggerFactory loggerFactory)
        {
            foreach (var package in extPackages.Packages)
            {
                if (package.EntryPoint is null)
                {
                    ThrowIfRequestedExtensionPackage(package);
                    continue;
                }

                var entryPointAssembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(package.EntryPoint, package.PackageFolder));

                // Find the type that implements IProjectExtension.
                var extensionType = entryPointAssembly.GetExportedTypes()
                                                      .FirstOrDefault(t =>
                                                        typeof(IExtensionEntryPoint).IsAssignableFrom(t) &&
                                                         t.IsClass && !t.IsAbstract);

                if (extensionType is null)
                {
                    ThrowIfRequestedExtensionPackage(package);
                    continue;
                }

                var getValidConstructor = extensionType.GetConstructors().Where(IsValidConstructor).FirstOrDefault();

                if (getValidConstructor is null)
                {
                    throw new ProjectConfigurationException($"Cannot load the entry point for the {package.PackageId} extension. " +
                                                             "Extension entry points must implement the IProjectExtension interface, and have a public constructor" +
                                                             "with only ILoggerFactory (optionally) as a constructor argument.");
                }

                extensions.Add(Construct(extensionType, loggerFactory));
            }

        }

        public string GetPackagePath(string packageId, params string[] directoryParts)
        {
            var package = extPackages.Packages.FirstOrDefault(p => p.PackageId == packageId);

            if (package is null)
            {
                throw new InvalidOperationException("The specified package has not been loaded. Is it referenced by your extension?");
            }

            return Path.GetFullPath(Path.Combine(directoryParts), package.PackageFolder);
        }

        public bool IsPackageLoaded(string packageId)
        {
            return extPackages.Packages.Any(p => p.PackageId == packageId);
        }

        private void ThrowIfRequestedExtensionPackage(PackageEntry package)
        {
            if (requiredPackages.Contains(package.PackageId))
            {
                throw new ProjectConfigurationException($"Could not locate entry point for requested extension {package.PackageId}.");
            }
        }

        private IExtensionEntryPoint Construct(Type extensionType, ILoggerFactory logFactory)
        {
            var constructor = extensionType.GetConstructor(new[] { typeof(ILoggerFactory) });

            if (constructor is object)
            {
                return (IExtensionEntryPoint)constructor.Invoke(new[] { logFactory });
            }

            return (IExtensionEntryPoint)Activator.CreateInstance(extensionType);
        }

        private bool IsValidConstructor(ConstructorInfo constructor)
        {
            var constructorArgs = constructor.GetParameters();

            if (constructorArgs.Any(x => x.ParameterType != typeof(ILoggerFactory)))
            {
                return false;
            }

            return true;
        }

        private class ExtLoadContext : AssemblyLoadContext
        {
            private readonly ExtensionPackages extFiles;

            public ExtLoadContext(ExtensionPackages extFiles)
                : base(true)
            {
                this.extFiles = extFiles;
            }

            protected override Assembly Load(AssemblyName assemblyName)
            {
                var dllName = assemblyName.Name + ".dll";

                // Find all DLLs that match.
                foreach (var package in extFiles.Packages)
                {
                    var matchingFile = package.LibFiles.FirstOrDefault(f => Path.GetFileName(f) == dllName);

                    if (matchingFile is object)
                    {
                        // Got it.
                        return LoadFromAssemblyPath(Path.GetFullPath(matchingFile, package.PackageFolder));
                    }
                }

                return null;
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                UnloadAndWipe();

                loadContext.Unload();
                loadContext = null!;

                var retryCount = 0;

                // Give the GC time to try and unload.
                // It is possible that during test execution (as opposed to compilation),
                // types will be loaded that cannot be unloaded. That's ok though, the important thing is that the language service
                // can dynamically unload the 'limited' foot-print in the context of the loaded steps.
                while (weakContextReference.IsAlive && retryCount < 10)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    retryCount++;
                }

                isDisposed = true;
            }
        }

        private void UnloadAndWipe()
        {
            foreach (var ext in extensions)
            {
                ext.Dispose();
            }

            // Empty the loaded extension list.
            extensions.Clear();
        }
    }
}
