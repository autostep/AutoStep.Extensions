﻿using System;
using AutoStep;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
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
