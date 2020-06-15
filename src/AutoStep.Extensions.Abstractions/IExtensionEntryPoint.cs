using System;
using Autofac;
using AutoStep.Execution;
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
        /// This method is invoked once before each test run, and can be used to customise the test execution process,
        /// add event handlers, register additional services into the DI container, or alter the configuration used for the test run.
        /// </summary>
        /// <param name="projectConfig">The loaded project configuration.</param>
        /// <param name="testRun">The test run instance that is about to start.</param>
        void ExtendExecution(IConfiguration projectConfig, TestRun testRun);
    }
}
