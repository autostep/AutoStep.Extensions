using System;
using AutoStep;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Extensions;
using AutoStep.Projects;
using LocalExtensionDependency;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestExtensionReferencesNewtonSoft
{
    public class TestEntryPoint : IExtensionEntryPoint
    {
        public void AttachToProject(IConfiguration projectConfig, Project project)
        {
            var instance = new MyClass();

            // Use newtonsoft.
            JsonConvert.DeserializeObject<JObject>("{}");
        }

        public void ConfigureExecutionServices(IConfiguration runConfiguration, IServicesBuilder servicesBuilder)
        {
        }

        public void Dispose()
        {
        }

        public void ExtendExecution(IConfiguration projectConfig, TestRun testRun)
        {
        }
    }
}
