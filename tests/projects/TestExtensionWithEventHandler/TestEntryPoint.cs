using Autofac;
using AutoStep;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Extensions;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;

namespace TestExtensionWithEventHandler
{

    public class TestEntryPoint : IExtensionEntryPoint
    {
        public void AttachToProject(IConfiguration projectConfig, Project project)
        {
        }

        public void Dispose()
        {
        }

        public void ExtendExecution(IConfiguration projectConfig, TestRun testRun)
        {
            testRun.Events.Add(new MyHandler());
        }
    }
}
