using System;
using Autofac;
using AutoStep.Execution;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Simple base class for extensions that can be derived from so the actual entry point doesn't have to implement
    /// all the methods.
    /// </summary>
    public abstract class BaseExtensionEntryPoint : IExtensionEntryPoint
    {
        private bool isDisposed = false;

        /// <summary>
        /// Gets the logger factory injected into the entry point at construction.
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseExtensionEntryPoint"/> class.
        /// </summary>
        /// <param name="logFactory">The logger factory.</param>
        protected BaseExtensionEntryPoint(ILoggerFactory logFactory)
        {
            LoggerFactory = logFactory;
        }

        /// <inheritdoc/>
        public virtual void AttachToProject(IConfiguration projectConfig, Project project)
        {
        }

        /// <inheritdoc/>
        public virtual void ConfigureExecutionServices(IConfiguration runConfiguration, ContainerBuilder containerBuilder)
        {
        }

        /// <inheritdoc/>
        public virtual void ExtendExecution(IConfiguration projectConfig, TestRun testRun)
        {
        }

        /// <summary>
        /// Disposes of the extension (and any resources it has allocated).
        /// </summary>
        /// <param name="disposing">True to unload managed resources. False to unload only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1063:Implement IDisposable Correctly",
            Justification = "It is correct, just a slight variant to make it easier on implementations.")]
        public void Dispose()
        {
            if (!isDisposed)
            {
                Dispose(true);

                isDisposed = true;
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="BaseExtensionEntryPoint"/> class.
        /// </summary>
        ~BaseExtensionEntryPoint()
        {
            Dispose(false);
        }
    }
}
