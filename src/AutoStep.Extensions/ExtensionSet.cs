using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
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

        private readonly ExtLoadContext loadContext;
        private readonly ExtensionPackages extPackages;

        private bool isDisposed;

        public ExtensionSet(IConfiguration projectConfig, ExtensionPackages packages)
        {
            loadContext = new ExtLoadContext(packages);
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

            private List<WeakReference> assemblyWeakReferences = new List<WeakReference>();

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
                        var assembly = LoadFromAssemblyPath(Path.GetFullPath(matchingFile, package.PackageFolder));

                        assemblyWeakReferences.Add(new WeakReference(assembly, trackResurrection: true));

                        return assembly;
                    }
                }

                return null;
            }

            public bool AnyWeakReferencesStillAlive()
            {
                return assemblyWeakReferences.Any(x => x.IsAlive);
            }
        }

        protected void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    foreach (var ext in extensions)
                    {
                        ext.Dispose();
                    }
                }

                loadContext.Unload();

                var retryCount = 0;

                // Give the GC time to unload the assembly.
                while (loadContext.AnyWeakReferencesStillAlive() && retryCount < 10)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    retryCount++;
                }

                isDisposed = true;
            }
        }

        ~ExtensionSet()
        {
            // Finalizer to make sure we can try to unload the assembly context.
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
