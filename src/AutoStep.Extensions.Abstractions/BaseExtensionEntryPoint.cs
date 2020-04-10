using System;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
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

        protected ILoggerFactory LoggerFactory { get; }

        protected BaseExtensionEntryPoint(ILoggerFactory logFactory)
        {
            LoggerFactory = logFactory;
        }

        public virtual void AttachToProject(IConfiguration projectConfig, Project project)
        {
        }

        public virtual void ConfigureExecutionServices(IConfiguration runConfiguration, IServicesBuilder servicesBuilder)
        {
        }

        public virtual void ExtendExecution(IConfiguration projectConfig, TestRun testRun)
        {
        }

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
