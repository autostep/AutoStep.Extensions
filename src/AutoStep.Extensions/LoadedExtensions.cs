using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using AutoStep.Extensions.Abstractions;
using Microsoft.Extensions.Logging;

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

            this.requiredPackages = extensions.Select(x => x.PackageId).ToList();
            this.extPackages = packages;

            loadContext = new ExtensionsAssemblyLoadContext(extPackages);
            weakContextReference = new WeakReference(loadContext);
        }

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

                var entryPointAssembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(package.EntryPoint, package.PackageFolder));

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

                var getValidConstructor = extensionType.GetConstructors().Where(IsValidConstructor).FirstOrDefault();

                if (getValidConstructor is null)
                {
                    throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.LoadedExtensions_CannotLoadEntryPoint, package.PackageId, typeof(TExtensionEntryPoint).Name));
                }

                extensions.Add(Construct(extensionType, loggerFactory));
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

        private static TExtensionEntryPoint Construct(Type extensionType, ILoggerFactory logFactory)
        {
            var constructor = extensionType.GetConstructor(new[] { typeof(ILoggerFactory) });

            if (constructor is object)
            {
                return (TExtensionEntryPoint)constructor.Invoke(new[] { logFactory });
            }

            try
            {
                return (TExtensionEntryPoint)Activator.CreateInstance(extensionType)!;
            }
            catch (Exception ex)
            {
                throw new ExtensionLoadException(string.Format(CultureInfo.CurrentCulture, Messages.LoadedExtensions_FailedToInstantiate, extensionType.FullName), ex);
            }
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
    }
}
