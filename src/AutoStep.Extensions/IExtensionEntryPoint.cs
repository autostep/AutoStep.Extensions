using System;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;

namespace AutoStep.Extensions
{
    public interface IExtensionEntryPoint : IDisposable
    {
        void AttachToProject(IConfiguration projectConfig, Project project);

        void ExtendExecution(IConfiguration projectConfig, TestRun testRun);

        void ConfigureExecutionServices(IConfiguration runConfiguration, IServicesBuilder servicesBuilder);
    }
}
