using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStep.Extensions.Abstractions
{
    /// <summary>
    /// Provides access to a set of loaded extension pakcages.
    /// </summary>
    public interface ILoadedExtensions : IDisposable
    {
        /// <summary>
        /// Gets the root directory for the set of extensions.
        /// </summary>
        string ExtensionsRootDir { get; }

        /// <summary>
        /// Gets the loaded package metadata.
        /// </summary>
        IEnumerable<IPackageMetadata> LoadedPackages { get; }

        /// <summary>
        /// Checks whether a package with a given ID has been loaded.
        /// </summary>
        /// <param name="packageId">The package ID.</param>
        /// <returns>True if available.</returns>
        bool IsPackageLoaded(string packageId);

        /// <summary>
        /// Gets an absolute path, given a package ID and the relative path inside the package.
        /// </summary>
        /// <param name="packageId">The package ID.</param>
        /// <param name="pathParts">The path components to combine.</param>
        /// <returns>An absolute path.</returns>
        string GetPackagePath(string packageId, params string[] pathParts);
    }

    /// <summary>
    /// Provides access to a loaded set of extension packages, and a loaded set of extension entry points.
    /// </summary>
    /// <typeparam name="TEntryPoint">The entry point type.</typeparam>
    /// <remarks>
    /// Disposing of an instance of this interface will unload the underlying assemblies and types if possible.
    /// </remarks>
    public interface ILoadedExtensions<TEntryPoint> : ILoadedExtensions
    {
        /// <summary>
        /// Gets the set of instantiated entry points.
        /// </summary>
        IEnumerable<TEntryPoint> ExtensionEntryPoints { get; }
    }
}
