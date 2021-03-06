﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoStep.Extensions.Abstractions;
using Microsoft.Extensions.Logging;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Contains the set of installed packages.
    /// </summary>
    public class InstalledExtensionPackages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstalledExtensionPackages"/> class.
        /// </summary>
        /// <param name="environment">The autostep host and execution environment.</param>
        /// <param name="packages">The set of package metadata.</param>
        public InstalledExtensionPackages(IAutoStepEnvironment environment, IReadOnlyList<IPackageMetadata> packages)
        {
            Environment = environment;
            Packages = packages;
        }

        /// <summary>
        /// Gets the autostep host and execution environment.
        /// </summary>
        public IAutoStepEnvironment Environment { get; }

        /// <summary>
        /// Gets the set of loaded package metadata.
        /// </summary>
        public IReadOnlyList<IPackageMetadata> Packages { get; }

        /// <summary>
        /// Get all packages that are considered top-level (i.e. were originally requested as configured extensions).
        /// </summary>
        /// <returns>The filtered package set.</returns>
        public IEnumerable<IPackageMetadata> GetTopLevelPackages()
        {
            return Packages.Where(x => x.DependencyType == PackageDependencyTypes.ExtensionPackage);
        }

        /// <summary>
        /// Load the extensions from this set of installed packages.
        /// </summary>
        /// <typeparam name="TEntryPoint">The entry point type to search for in the extension assemblies.</typeparam>
        /// <param name="loggerFactory">A logger factory.</param>
        /// <returns>A set of loaded extensions.</returns>
        public ILoadedExtensions<TEntryPoint> LoadExtensionsFromPackages<TEntryPoint>(ILoggerFactory loggerFactory)
            where TEntryPoint : IDisposable
        {
            var loadedExtensions = new LoadedExtensions<TEntryPoint>(this);

            loadedExtensions.LoadEntryPoints(loggerFactory);

            return loadedExtensions;
        }
    }
}
