using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;

namespace AutoStep.Extensions.NuGetExtensions
{
    /// <summary>
    /// Wraps another <see cref="IInstallablePackageSet"/>, and updates the dependency cache JSON file when that set has installed.
    /// </summary>
    internal class WriteCacheOnInstallPackageSet : IInstallablePackageSet
    {
        private readonly string dependencyJsonFile;
        private readonly IHostContext hostContext;
        private readonly ExtensionResolveContext resolveContext;
        private readonly IInstallablePackageSet wrappedPackageSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteCacheOnInstallPackageSet"/> class.
        /// </summary>
        /// <param name="dependencyJsonFile">The path to the dependency JSON file.</param>
        /// <param name="hostContext">The host context.</param>
        /// <param name="resolveContext">The extension resolve context.</param>
        /// <param name="wrappedPackageSet">The backing set.</param>
        public WriteCacheOnInstallPackageSet(string dependencyJsonFile, IHostContext hostContext, ExtensionResolveContext resolveContext, IInstallablePackageSet wrappedPackageSet)
        {
            this.dependencyJsonFile = dependencyJsonFile;
            this.hostContext = hostContext;
            this.resolveContext = resolveContext;
            this.wrappedPackageSet = wrappedPackageSet;
        }

        /// <inheritdoc/>
        public IEnumerable<string> PackageIds => wrappedPackageSet.PackageIds;

        /// <inheritdoc/>
        public bool IsValid => true;

        /// <inheritdoc/>
        public Exception? Exception => null;

        /// <inheritdoc/>
        public async ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            using (var dependencyFileLock = await TakePathLock(dependencyJsonFile, cancelToken))
            {
                var result = await wrappedPackageSet.InstallAsync(cancelToken);

                SaveExtensionDependencyContext(resolveContext, result);

                return result;
            }
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

        /// <summary>
        /// Creates a hash of an absolute path, that we can then use in another file name.
        /// </summary>
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

        private void SaveExtensionDependencyContext(ExtensionResolveContext resolveContext, InstalledExtensionPackages packages)
        {
            // Target IDs to be saved in the dependency context needs to include any additional packages required
            // by other extension resolvers.
            var targetIds = new HashSet<string>(resolveContext.PackageExtensions.Select(x => x.Package!)
                                                                                .Concat(resolveContext.AdditionalPackagesRequired.Select(x => x.Id)));

            var newDepContext = new DependencyContext(
                hostContext.Target,
                CompilationOptions.Default,
                Enumerable.Empty<CompilationLibrary>(),
                packages.Packages.Select(p => RuntimeLibraryFromPackage(targetIds, p, packages.Packages)),
                Enumerable.Empty<RuntimeFallbacks>());

            // Write the dependency file.
            var dependencyWriter = new DependencyContextWriter();

            using (var fileStream = File.Open(dependencyJsonFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                dependencyWriter.Write(newDepContext, fileStream);
            }
        }

        private RuntimeLibrary RuntimeLibraryFromPackage(ISet<string> topLevelPackageIds, IPackageMetadata package, IReadOnlyList<IPackageMetadata> allPackages)
        {
            var runtimeAssetGroup = new RuntimeAssetGroup(hostContext.Target.Runtime, package.LibFiles.Where(f => Path.GetExtension(f) == ".dll")
                                                                                         .Select(f => GetRuntimeFile(package, f)));

            return new RuntimeLibrary(
                      topLevelPackageIds.Contains(package.PackageId) ? ExtensionRuntimeLibraryTypes.RootPackage : ExtensionRuntimeLibraryTypes.Dependency,
                      package.PackageId,
                      package.PackageVersion,
                      null,
                      new[] { runtimeAssetGroup },
                      new List<RuntimeAssetGroup>(),
                      Enumerable.Empty<ResourceAssembly>(),
                      package.Dependencies.Select(dep => allPackages.FirstOrDefault(p => p.PackageId == dep))
                                    .Where(d => d is object)
                                    .Select(d => new Dependency(d.PackageId, d.PackageVersion)),
                      true);
        }

        private static RuntimeFile GetRuntimeFile(IPackageMetadata packageEntry, string file)
        {
            var fullPath = Path.GetFullPath(file, packageEntry.PackageFolder);

            var assemblyName = AssemblyName.GetAssemblyName(fullPath);

            var fileVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);

            return new RuntimeFile(file, assemblyName.Version?.ToString(), fileVersionInfo.FileVersion);
        }
    }
}
