using System;
using System.Collections.Generic;

namespace AutoStep.Extensions.Watch
{
    /// <summary>
    /// Tracks project packages. Maintains the necessary file-system watchers.
    /// </summary>
    /// <remarks>
    /// Starts suspended, must be explicitly 'resumed' to capture notifications.
    /// </remarks>
    public sealed class PackageCollectionWatcher : IDisposable
    {
        private readonly Dictionary<string, IPackageWatcher> packageWatchers = new Dictionary<string, IPackageWatcher>();
        private readonly Func<ILocalExtensionPackageMetadata, IPackageWatcher> watcherFactory;
        private readonly object sync = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageCollectionWatcher"/> class.
        /// </summary>
        public PackageCollectionWatcher()
        {
            watcherFactory = meta => new PackageWatcher(meta);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageCollectionWatcher"/> class.
        /// </summary>
        /// <param name="watcherFactory">A custom <see cref="IPackageWatcher"/> factory.</param>
        public PackageCollectionWatcher(Func<ILocalExtensionPackageMetadata, IPackageWatcher> watcherFactory)
        {
            this.watcherFactory = watcherFactory;
        }

        /// <summary>
        /// Synchronise a set of packages with the internal set being monitored.
        /// </summary>
        /// <param name="packages">The set of packages.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Reliability",
            "CA2000:Dispose objects before losing scope",
            Justification = "Existing watchers are still tracked by the dictionary, so do not need to be disposed here.")]
        public void SyncPackages(IEnumerable<ILocalExtensionPackageMetadata> packages)
        {
            if (packages is null)
            {
                throw new ArgumentNullException(nameof(packages));
            }

            // Make sure we have the right file watchers and metadata for each watched directory.
            var foundWatchers = new Dictionary<string, IPackageWatcher>(packageWatchers);

            // Go through the set of packages, looking for an active watch context to update.
            foreach (var packageMeta in packages)
            {
                if (packageMeta.WatchMode == PackageWatchMode.None)
                {
                    // No watch, no point adding a tracker.
                    continue;
                }

                if (foundWatchers.Remove(packageMeta.SourceProjectFolder, out var existingWatcher))
                {
                    existingWatcher.Update(packageMeta);
                }
                else
                {
                    // Create and add.
                    var context = watcherFactory(packageMeta);
                    context.OnPackageChange += OnChange;
                    packageWatchers.Add(packageMeta.SourceProjectFolder, context);
                }
            }

            // Strip out any old contexts.
            foreach (var unusedWatcher in foundWatchers)
            {
                packageWatchers.Remove(unusedWatcher.Key);
                unusedWatcher.Value.Dispose();
                unusedWatcher.Value.OnPackageChange -= OnChange;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current collection is 'dirty'. Value is reset when <see cref="Reset"/> is called.
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the package collection watcher is suspended (i.e. will not raise events).
        /// </summary>
        public bool Suspended { get; private set; } = true;

        /// <summary>
        /// Event raised when a watched package becomes dirty. The event will only be raised once before <see cref="Reset"/> must be called to allow subsequent events to be raised.
        /// </summary>
        public event EventHandler<ILocalExtensionPackageMetadata>? OnWatchedPackageDirty;

        /// <summary>
        /// Reset the state of the collection watcher (resets <see cref="IsDirty"/> to false).
        /// </summary>
        public void Reset()
        {
            lock (sync)
            {
                IsDirty = false;

                foreach (var packageWatch in packageWatchers.Values)
                {
                    packageWatch.Reset();
                }
            }
        }

        /// <summary>
        /// Suspends the raising of events.
        /// </summary>
        public void Suspend()
        {
            lock (sync)
            {
                if (!Suspended)
                {
                    Suspended = true;
                }
            }
        }

        /// <summary>
        /// Start raising events.
        /// </summary>
        public void Start()
        {
            lock (sync)
            {
                Suspended = false;

                foreach (var packageWatch in packageWatchers.Values)
                {
                    packageWatch.Start();
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (sync)
            {
                foreach (var item in packageWatchers)
                {
                    item.Value.Dispose();
                    item.Value.OnPackageChange -= OnChange;
                }

                packageWatchers.Clear();
            }
        }

        private void OnChange(object? sender, ILocalExtensionPackageMetadata dirtyPackage)
        {
            if (!Suspended && !IsDirty)
            {
                var raise = false;

                lock (sync)
                {
                    if (!Suspended && !IsDirty)
                    {
                        IsDirty = true;
                        raise = true;
                    }
                }

                // Raise outside the lock so we don't hold onto it too long.
                if (raise)
                {
                    OnWatchedPackageDirty?.Invoke(this, dirtyPackage);
                }
            }
        }
    }
}
