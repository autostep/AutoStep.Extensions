using System.Collections.Generic;
using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Represents a host context (i.e. a host that loads extensions).
    /// </summary>
    internal interface IHostContext
    {
        /// <summary>
        /// Gets the target nuget framework of the host.
        /// </summary>
        NuGetFramework TargetFramework { get; }

        /// <summary>
        /// Gets the known dependency TargetInfo for the host.
        /// </summary>
        TargetInfo Target { get; }

        /// <summary>
        /// Gets the target framework name of the host.
        /// </summary>
        string FrameworkName { get; }

        /// <summary>
        /// Gets the host environment.
        /// </summary>
        public IAutoStepEnvironment Environment { get; }

        /// <summary>
        /// Gets a tag used to filter the nuget packages that should be checked for an extension entry point.
        /// </summary>
        string? EntryPointPackageTag { get; }

        /// <summary>
        /// Checks whether a given NuGet package reference is supplied by the host,
        /// and should not be downloaded from NuGet.
        /// </summary>
        /// <param name="dep">The nuget package dependency.</param>
        /// <returns>True if supplied by the host, false otherwise.</returns>
        bool DependencySuppliedByHost(PackageDependency dep);

        /// <summary>
        /// Get the set of framework-specific files (based on the host's framework) from a given set of framework-specific groups.
        /// </summary>
        /// <param name="frameworkGroups">The set of framework groups.</param>
        /// <returns>The set of relative file paths.</returns>
        IEnumerable<string> GetFrameworkFiles(IEnumerable<FrameworkSpecificGroup> frameworkGroups);

        /// <summary>
        /// Get the set of relative paths for the DLLs in a given runtime library.
        /// </summary>
        /// <param name="library">The runtime library from a loaded dependency context.</param>
        /// <returns>The set of relative file paths.</returns>
        IEnumerable<string>? GetRuntimeLibraryLibPaths(RuntimeLibrary library);
    }
}
