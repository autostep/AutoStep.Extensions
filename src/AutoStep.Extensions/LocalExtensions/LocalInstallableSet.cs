using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions.LocalExtensions
{
    /// <summary>
    /// Defines an installable set of packages backed by built MSBuild projects.
    /// </summary>
    internal class LocalInstallableSet : IInstallablePackageSet
    {
        private readonly IReadOnlyList<LocalProjectPackage> packages;
        private readonly IHostContext hostContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalInstallableSet"/> class.
        /// </summary>
        /// <param name="packages">The set of project packages.</param>
        /// <param name="hostContext">The host context.</param>
        public LocalInstallableSet(IReadOnlyList<LocalProjectPackage> packages, IHostContext hostContext)
        {
            this.packages = packages;
            this.hostContext = hostContext;
        }

        /// <inheritdoc/>
        public IEnumerable<string> PackageIds => packages.Select(x => x.ProjectName);

        /// <inheritdoc/>
        public bool IsValid => true;

        /// <inheritdoc/>
        public Exception? Exception => null;

        /// <inheritdoc/>
        public async ValueTask<InstalledExtensionPackages> InstallAsync(CancellationToken cancelToken)
        {
            // Install the packages (copy the files over).
            var rootExtensionDir = hostContext.ExtensionsDirectory;

            var packageMetadata = new List<IPackageMetadata>();

            // Go through the projects and copy them.
            foreach (var project in packages)
            {
                var destinationDirectory = Path.Combine(rootExtensionDir, project.ProjectName + "." + project.ProjectVersion);

                // Delete the target before copying.
                if (Directory.Exists(destinationDirectory))
                {
                    Directory.Delete(destinationDirectory, true);
                }

                // Copy the source directory.
                await CopyDirectoryAsync(project.BinaryDirectory, destinationDirectory, cancelToken).ConfigureAwait(false);

                string? entryPoint = null;

                var directoryInfo = new DirectoryInfo(destinationDirectory);

                var libFiles = directoryInfo.GetFiles("*.dll").Select(x => x.Name);

                if (File.Exists(Path.Combine(destinationDirectory, project.EntryPointDllName)))
                {
                    // Treat as the entry point.
                    entryPoint = project.EntryPointDllName;
                }

                packageMetadata.Add(new PackageMetadata(
                    project.ProjectName,
                    project.ProjectVersion,
                    destinationDirectory,
                    entryPoint,
                    libFiles,
                    PackageDependencyTypes.ExtensionPackage));
            }

            return new InstalledExtensionPackages(packageMetadata);
        }

        /// <summary>
        /// Recursive directory copy behaviour, with async file copies.
        /// </summary>
        private async Task CopyDirectoryAsync(string sourceDirectory, string targetDirectory, CancellationToken cancelToken)
        {
            const FileOptions CopyFileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            const int BufferSize = 4096;

            Directory.CreateDirectory(targetDirectory);

            var sourceInfo = new DirectoryInfo(sourceDirectory);

            foreach (var fileSystemEntry in sourceInfo.EnumerateFileSystemInfos())
            {
                cancelToken.ThrowIfCancellationRequested();

                if (fileSystemEntry is DirectoryInfo directory)
                {
                    var destination = Path.Combine(targetDirectory, directory.Name);

                    await CopyDirectoryAsync(directory.FullName, destination, cancelToken).ConfigureAwait(false);
                }
                else if (fileSystemEntry is FileInfo file)
                {
                    var destination = Path.Combine(targetDirectory, file.Name);

                    using (var sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, CopyFileOptions))
                    using (var destinationStream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, CopyFileOptions))
                    {
                        await sourceStream.CopyToAsync(destinationStream, BufferSize, cancelToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
