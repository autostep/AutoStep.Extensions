#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable CA1063 // Implement IDisposable Correctly

namespace LocalExtension
{
    using System;
    using AutoStep.Execution;
    using AutoStep.Execution.Dependency;
    using AutoStep.Extensions;
    using AutoStep.Projects;
    using LocalExtensionDependency;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Test.
    /// </summary>
    public class TestEntryPoint : IExtensionEntryPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestEntryPoint"/> class.
        /// </summary>
        /// <param name="logFactory">Expected injected log factory.</param>
        public TestEntryPoint(ILoggerFactory logFactory)
        {
            if (logFactory is null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }
        }

        /// <inheritdoc/>
        public void AttachToProject(IConfiguration projectConfig, Project project)
        {
            var instance = new MyClass();

            // Use newtonsoft.
            JsonConvert.DeserializeObject<JObject>("{}");
        }

        /// <inheritdoc/>
        public void ConfigureExecutionServices(IConfiguration runConfiguration, IServicesBuilder servicesBuilder)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public void ExtendExecution(IConfiguration projectConfig, TestRun testRun)
        {
        }
    }
}
