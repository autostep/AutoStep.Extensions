using System;

namespace AutoStep.Extensions.Watch
{
    /// <summary>
    /// Defines a package watcher, that monitors changes on individual package sources.
    /// </summary>
    public interface IPackageWatcher : IDisposable
    {
        /// <summary>
        /// Start watching a package.
        /// </summary>
        void Start();

        /// <summary>
        /// Reset the state of the watcher so that new events will be raised.
        /// </summary>
        void Reset();

        /// <summary>
        /// Event that is raised when the package source is modified. This event will only be raised once before a Reset is called. Changes
        /// after the Reset will then cause the event to fire agains.
        /// </summary>
        event EventHandler<ILocalExtensionPackageMetadata>? OnPackageChange;

        /// <summary>
        /// Updates the package metadata for a given watcher.
        /// </summary>
        /// <param name="metadata">The package metadata.</param>
        void Update(ILocalExtensionPackageMetadata metadata);
    }
}
