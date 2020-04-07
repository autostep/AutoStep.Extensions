using System;
using System.Collections;
using System.Collections.Generic;
using AutoStep.Execution;
using AutoStep.Execution.Dependency;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;

namespace AutoStep.Extensions
{
    public interface IExtensionSet : IDisposable
    {
        string ExtensionsRootDir { get; }

        IEnumerable<IPackageMetadata> LoadedPackages { get; }

        void AttachToProject(IConfiguration configuration, Project project);

        void ConfigureExtensionServices(IConfiguration configuration, IServicesBuilder builder);

        void ExtendExecution(IConfiguration configuration, TestRun testRun);
    }
}
