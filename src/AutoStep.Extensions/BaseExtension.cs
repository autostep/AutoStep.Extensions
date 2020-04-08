using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;

namespace AutoStep.Extensions
{
    public abstract class BaseExtension : IExtensionEntryPoint
    {
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
