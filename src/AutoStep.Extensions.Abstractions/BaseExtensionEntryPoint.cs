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

        public virtual void Dispose()
        {
        }
    }
}
