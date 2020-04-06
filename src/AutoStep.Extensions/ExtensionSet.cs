using System;
using System.Collections.Generic;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Projects;

namespace AutoStep.Extensions
{
    public class ExtensionSet : IDisposable
    {
        private readonly List<IProjectExtension> extensions = new List<IProjectExtension>();
        private readonly ProjectConfiguration projectConfiguration;

        public ExtensionSet(ProjectConfiguration projectConfiguration)
        {
            this.projectConfiguration = projectConfiguration;
        }

        /// <summary>
        /// Called prior to execution.
        /// </summary>
        /// <param name="builder">The services builder.</param>
        /// <param name="configuration">The test run configuration.</param>
        public void ConfigureExtensionServices(IServicesBuilder builder, RunConfiguration configuration)
        {
            foreach (var ext in extensions)
            {
                ext.ConfigureExecutionServices(projectConfiguration, configuration, builder);
            }
        }

        public void AttachToProject(Project project)
        {
            foreach (var ext in extensions)
            {
                ext.AttachToProject(projectConfiguration, project);
            }
        }

        public void ExtendExecution(RunConfiguration runConfig, TestRun testRun)
        {
            foreach (var ext in extensions)
            {
                ext.ExtendExecution(projectConfiguration, runConfig, testRun);
            }
        }

        public void Add(IProjectExtension extensionEntryPoint)
        {
            extensions.Add(extensionEntryPoint);
        }

        public virtual void Dispose()
        {
            foreach (var ext in extensions)
            {
                ext.Dispose();
            }
        }
    }
}
