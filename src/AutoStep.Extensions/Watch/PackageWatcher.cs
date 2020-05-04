using System;
using System.IO;
using System.Linq;

namespace AutoStep.Extensions.Watch
{
    /// <summary>
    /// Provides the default package watcher, which watches files within a package directory and raises the <see cref="OnPackageChange"/> event
    /// when a file changes (determined by the <see cref="ILocalExtensionPackageMetadata.WatchMode"/>).
    /// </summary>
    internal class PackageWatcher : IPackageWatcher
    {
        private ILocalExtensionPackageMetadata metadata;
        private FileSystemWatcher? watcher;
        private bool isDirty;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageWatcher"/> class.
        /// </summary>
        /// <param name="metadata">The initial metadata.</param>
        public PackageWatcher(ILocalExtensionPackageMetadata metadata)
        {
            this.metadata = metadata;
        }

        /// <inheritdoc/>
        public event EventHandler<ILocalExtensionPackageMetadata>? OnPackageChange;

        /// <inheritdoc/>
        public void Update(ILocalExtensionPackageMetadata metadata)
        {
            this.metadata = metadata;
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (watcher is null)
            {
                var newWatcher = new FileSystemWatcher(metadata.SourceProjectFolder)
                {
                    IncludeSubdirectories = true,

                    NotifyFilter = NotifyFilters.LastWrite |
                                   NotifyFilters.FileName |
                                   NotifyFilters.DirectoryName,
                };

                newWatcher.Changed += OnChanged;
                newWatcher.Created += OnChanged;
                newWatcher.Deleted += OnChanged;
                newWatcher.Renamed += OnRename;

                watcher = newWatcher;

                newWatcher.EnableRaisingEvents = true;
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            isDirty = false;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            watcher?.Dispose();
            watcher = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PackageWatcher"/> class.
        /// </summary>
        ~PackageWatcher()
        {
            watcher?.Dispose();
            watcher = null;
        }

        private void OnRename(object sender, RenamedEventArgs e)
        {
            if (FileIsWatched(e.FullPath) || FileIsWatched(e.OldFullPath))
            {
                isDirty = true;
                OnPackageChange?.Invoke(this, metadata);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (FileIsWatched(e.FullPath))
            {
                isDirty = true;
                OnPackageChange?.Invoke(this, metadata);
            }
        }

        private bool FileIsWatched(string fullPath)
        {
            if (isDirty)
            {
                // Already dirty, not worth testing for it.
                return false;
            }

            // Project file?
            if (fullPath.Equals(metadata.SourceProjectFile, StringComparison.CurrentCultureIgnoreCase))
            {
                // Project file change.
                return true;
            }

            if (metadata.WatchMode == PackageWatchMode.Full)
            {
                // In full mode, we will watch any source files.
                if (metadata.SourceFiles.Any(sourcePath => sourcePath.Equals(fullPath, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return true;
                }
            }

            // If not in full mode, we will watch any files in the binary directory.
            var binaryDirectory = metadata.SourceBinaryDirectory;

            // How to know if the file is in the binary directory?
            // Get the directory name until the directory is the same as the project directory.
            return IsFileInDirectory(fullPath, binaryDirectory);
        }

        private bool IsFileInDirectory(string filePath, string containingDirectory)
        {
            // If the path starts with the specified containing directory, then they should be the same, provided we've
            // terminated the containingDirectory.
            return filePath.StartsWith(containingDirectory, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
