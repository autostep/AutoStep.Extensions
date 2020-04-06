using System;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Projects;

namespace AutoStep.Extensions
{
    public interface IProjectExtension : IDisposable
    {
        void AttachToProject(ProjectConfiguration projectConfig, Project project);

        void ExtendExecution(ProjectConfiguration projectConfig, RunConfiguration runConfig, TestRun testRun);

        void ConfigureExecutionServices(ProjectConfiguration projectConfig, RunConfiguration runConfig, IServicesBuilder servicesBuilder);
    }
}
