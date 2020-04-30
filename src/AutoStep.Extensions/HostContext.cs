﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Encapsulates the host context.
    /// </summary>
    internal class HostContext : IHostContext
    {
        private readonly FrameworkReducer frameworkReducer;
        private readonly DependencyContext hostDependencyContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="HostContext"/> class.
        /// </summary>
        /// <param name="hostAssembly">An assembly indicating the host context.</param>
        /// <param name="rootDirectory">The root directory of the host project.</param>
        /// <param name="packageInstallDirectory">The directory to install packages in.</param>
        /// <param name="extensionPackageTag">An optional extension packages tag.</param>
        public HostContext(Assembly hostAssembly, string rootDirectory, string packageInstallDirectory, string? extensionPackageTag)
        {
            if (hostAssembly is null)
            {
                throw new ArgumentNullException(nameof(hostAssembly));
            }

            if (!Path.IsPathFullyQualified(rootDirectory))
            {
                throw new ArgumentException(Messages.HostContext_DirectoryMustBeAbsolute, nameof(rootDirectory));
            }

            if (!Path.IsPathFullyQualified(packageInstallDirectory))
            {
                throw new ArgumentException(Messages.HostContext_DirectoryMustBeAbsolute, nameof(packageInstallDirectory));
            }

            hostDependencyContext = DependencyContext.Load(hostAssembly);
            FrameworkName = hostDependencyContext.Target.Framework;
            TargetFramework = NuGetFramework.ParseFrameworkName(FrameworkName, DefaultFrameworkNameProvider.Instance);
            frameworkReducer = new FrameworkReducer();
            EntryPointPackageTag = extensionPackageTag;

            RootDirectory = rootDirectory;
            ExtensionsDirectory = packageInstallDirectory;
        }

        /// <inheritdoc/>
        public NuGetFramework TargetFramework { get; }

        /// <inheritdoc/>
        public string FrameworkName { get; }

        /// <inheritdoc/>
        public TargetInfo Target => hostDependencyContext.Target;

        /// <inheritdoc/>
        public string? EntryPointPackageTag { get; }

        /// <inheritdoc/>
        public string RootDirectory { get; }

        /// <inheritdoc/>
        public string ExtensionsDirectory { get; }

        /// <inheritdoc/>
        public IEnumerable<string> GetFrameworkFiles(IEnumerable<FrameworkSpecificGroup> frameworkGroup)
        {
            var nearest = frameworkReducer.GetNearest(TargetFramework, frameworkGroup.Select(x => x.TargetFramework));

            var selectedItems = frameworkGroup.Where(x => x.TargetFramework.Equals(nearest))
                                              .SelectMany(x => x.Items);

            return selectedItems;
        }

        /// <inheritdoc/>
        public IEnumerable<string>? GetRuntimeLibraryLibPaths(RuntimeLibrary library)
        {
            var targetRuntimeGroup = library.RuntimeAssemblyGroups.FirstOrDefault(t => string.IsNullOrEmpty(t.Runtime) || t.Runtime == FrameworkName);

            if (targetRuntimeGroup == null)
            {
                return null;
            }

            return targetRuntimeGroup.AssetPaths;
        }

        /// <inheritdoc/>
        public bool DependencySuppliedByHost(PackageDependency dep)
        {
            // Is the package provided by the fr
            return RuntimeProvidedPackages.IsPackageProvidedByRuntime(dep.Id) || LibrarySuppliedByHost(dep);
        }

        private bool LibrarySuppliedByHost(PackageDependency dep)
        {
            // See if a runtime library with the same ID as the package is available in the host's runtime libraries.
            var runtimeLib = hostDependencyContext.RuntimeLibraries.FirstOrDefault(r => r.Name == dep.Id);

            if (runtimeLib is object)
            {
                // What version of the library is the host using?
                var parsedLibVersion = NuGetVersion.Parse(runtimeLib.Version);

                if (parsedLibVersion.IsPrerelease)
                {
                    // Always use pre-releases in the host, otherwise it becomes
                    // a nightmare to develop across a couple of different projects.
                    return true;
                }
                else
                {
                    // Does the host version satisfy the version range of the requested package.
                    return dep.VersionRange.Satisfies(parsedLibVersion);
                }
            }

            return false;
        }
    }
}
