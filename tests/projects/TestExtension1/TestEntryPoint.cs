using AutoStep.Execution;
using AutoStep.Extensions;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;

namespace TestExtension1
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
        }
    }
}
