using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoStep.Extensions.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Contains a set of loaded extensions (and references to the appropriate entry point).
    /// </summary>
    /// <typeparam name="TExtensionEntryPoint">The entry point type.</typeparam>
    internal sealed class LoadedExtensions<TExtensionEntryPoint> : ILoadedExtensions<TExtensionEntryPoint>
        where TExtensionEntryPoint : IDisposable
    {
        private readonly List<TExtensionEntryPoint> extensions = new List<TExtensionEntryPoint>();

        private readonly IReadOnlyList<string> requiredPackages;
        private readonly InstalledExtensionPackages extPackages;

        private readonly WeakReference weakContextReference;
        private ExtensionsAssemblyLoadContext loadContext;

        private bool isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadedExtensions{TExtensionEntryPoint}"/> class.
        /// </summary>
        /// <param name="packages">The set of packages to load from.</param>
        public LoadedExtensions(InstalledExtensionPackages packages)
        {
            isDisposed = false;

            // Get the set of required extensions.
            var extensions = packages.GetTopLevelPackages();

            this.Environment = packages.Environment;

            this.requiredPackages = extensions.Select(x => x.PackageId).ToList();
            this.extPackages = packages;

            loadContext = new ExtensionsAssemblyLoadContext(extPackages);
            weakContextReference = new WeakReference(loadContext);
        }

        /// <inheritdoc/>
        public IAutoStepEnvironment Environment { get; }

        /// <inheritdoc/>
        public IEnumerable<IPackageMetadata> Packages => extPackages.Packages;

        /// <inheritdoc/>
        public IEnumerable<TExtensionEntryPoint> ExtensionEntryPoints => extensions;

        /// <summary>
        /// Load the set of available entry points.
        /// </summary>
        /// <param name="loggerFactory">The logging factory (which can be passed to the constructor of each entry point).</param>
        public void LoadEntryPoints(ILoggerFactory loggerFactory)
        {
            foreach (var package in extPackages.Packages)
            {
                if (package.EntryPoint is null)
                {
                    ThrowIfRequestedExtensionPackage(package);
                    continue;
                }

                var entryPointAssembly = loadContext.LoadFromAssemblyPathWithoutLock(Path.GetFullPath(package.EntryPoint, package.PackageFolder));

                // Find the type that implements the entry point..
                var extensionType = entryPointAssembly.GetExportedTypes()
                                                      .FirstOrDefault(t =>
                                                        typeof(TExtensionEntryPoint).IsAssignableFrom(t) &&
                                                         t.IsClass && !t.IsAbstract);

                if (extensionType is null)
                {
                    ThrowIfRequestedExtensionPackage(package);
                    continue;
                }

                var validConstructor = extensionType.GetConstructors().Where(IsValidConstructor).FirstOrDefault();

                if (validConstructor is null)
                {
                    throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.LoadedExtensions_CannotLoadEntryPoint, package.PackageId, typeof(TExtensionEntryPoint).Name));
                }

                extensions.Add(Construct(validConstructor, extensionType, loggerFactory, Environment));
            }
        }

        /// <inheritdoc/>
        public string GetPackagePath(string packageId, params string[] directoryParts)
        {
            var package = extPackages.Packages.FirstOrDefault(p => p.PackageId == packageId);

            if (package is null)
            {
                throw new InvalidOperationException(Messages.LoadedExtensions_PackageNotLoaded);
            }

            return Path.GetFullPath(Path.Combine(directoryParts), package.PackageFolder);
        }

        /// <inheritdoc/>
        public bool IsPackageLoaded(string packageId)
        {
            return extPackages.Packages.Any(p => p.PackageId == packageId);
        }

        private void ThrowIfRequestedExtensionPackage(IPackageMetadata package)
        {
            if (requiredPackages.Contains(package.PackageId))
            {
                throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.LoadedExtensions_EntryPointNotAvailable, package.PackageId));
            }
        }

        private static TExtensionEntryPoint Construct(ConstructorInfo? constructor, Type extensionType, ILoggerFactory logFactory, IAutoStepEnvironment environment)
        {
            try
            {
                if (constructor is object)
                {
                    var constructorArgs = constructor.GetParameters();

                    var parameters = new object?[constructorArgs.Length];

                    for (var argIdx = 0; argIdx < constructorArgs.Length; argIdx++)
                    {
                        var paramType = constructorArgs[argIdx].ParameterType;

                        if (paramType == typeof(ILoggerFactory))
                        {
                            parameters[argIdx] = logFactory;
                        }
                        else if (paramType == typeof(IAutoStepEnvironment))
                        {
                            parameters[argIdx] = environment;
                        }
                        else
                        {
                            throw new InvalidOperationException(Messages.LoadedExtensions_BadConstructor);
                        }
                    }

                    return (TExtensionEntryPoint)constructor.Invoke(parameters);
                }

                return (TExtensionEntryPoint)Activator.CreateInstance(extensionType)!;
            }
            catch (Exception ex)
            {
                throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.LoadedExtensions_FailedToInstantiate, extensionType.FullName), ex);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!isDisposed)
            {
                // Clear the extensions in a separate block so that the references can get cleaned up.
                ClearExtensions();

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

        private void ClearExtensions()
        {
            foreach (var ext in extensions)
            {
                ext.Dispose();
            }

            // Empty the loaded extension list.
            extensions.Clear();

            loadContext.Unload();
            loadContext = null!;
        }

        private bool IsValidConstructor(ConstructorInfo constructor)
        {
            var constructorArgs = constructor.GetParameters();

            if (constructorArgs.Any(x => x.ParameterType != typeof(ILoggerFactory) && x.ParameterType != typeof(IAutoStepEnvironment)))
            {
                return false;
            }

            return true;
        }
    }
}
