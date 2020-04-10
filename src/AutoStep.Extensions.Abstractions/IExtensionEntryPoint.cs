using System;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines the interface implemented by all extension entry point classes.
    /// </summary>
    /// <remarks>
    /// Each extension should have a single concrete implementation of this interface, which will be invoked
    /// at various times during the lifecycle of the extension.
    ///
    /// When the extension's <see cref="IDisposable.Dispose"/> implementation is invoked, the extension is being unloaded.
    /// Any background operations or held references should be unloaded before it returns.
    /// </remarks>
    public interface IExtensionEntryPoint : IDisposable
    {
        /// <summary>
        /// This method is invoked when the extension is first loaded and attached to the project.
        /// </summary>
        /// <remarks>
        /// Implementations should use this method to:
        ///  - Add custom steps.
        ///  - Add interaction methods.
        /// </remarks>
        /// <param name="projectConfig">The loaded project configuration.</param>
        /// <param name="project">The project to attach to.</param>
        void AttachToProject(IConfiguration projectConfig, Project project);

        /// <summary>
        /// This method is invoked once before each test run (and before <see cref="ConfigureExecutionServices(IConfiguration, IServicesBuilder)"/>),
        /// and can be used to customise the test execution process, add event handlers or alter the configuration used for the test run.
        /// </summary>
        /// <param name="projectConfig">The loaded project configuration.</param>
        /// <param name="testRun">The test run instance that is about to start.</param>
        void ExtendExecution(IConfiguration projectConfig, TestRun testRun);

        /// <summary>
        /// This method is invoked at the start of test execution to allow an extension to register methods into the DI container.
        /// </summary>
        /// <param name="runConfiguration">The configuration for the test run.</param>
        /// <param name="servicesBuilder">The services builder, in which you can register services for dependency injection during the run.</param>
        /// <remarks>
        /// Any services registered here will be available for injection into any of the interaction method or step binding classes that were registered
        /// in <see cref="AttachToProject(IConfiguration, Project)"/>.
        /// </remarks>
        void ConfigureExecutionServices(IConfiguration runConfiguration, IServicesBuilder servicesBuilder);
    }
}
