using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoStep.Extensions.Abstractions;
using AutoStep.Extensions.Tests.Shared;
using AutoStep.Projects;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace AutoStep.Extensions.IntegrationTests.Utils
{
    public abstract class BaseExtensionTests : TestLoggingBase
    {
        public BaseExtensionTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        protected class ExtensionTestContext : IDisposable
        {
            public ExtensionTestContext(
                string rootDirectory,
                string packageInstallDir,
                IConfiguration configuration,
                ISourceSettings sources,
                IEnumerable<PackageExtensionConfiguration> extensions,
                IEnumerable<FolderExtensionConfiguration> folderExtensions)
            {
                Environment = new AutoStepEnvironment(rootDirectory, packageInstallDir);
                Configuration = configuration;
                Sources = sources;
                Extensions = extensions;
                FolderExtensions = folderExtensions;
            }

            public IAutoStepEnvironment Environment { get; }

            public IConfiguration Configuration { get; }

            public ISourceSettings Sources { get; }

            public IEnumerable<PackageExtensionConfiguration> Extensions { get; }

            public IEnumerable<FolderExtensionConfiguration> FolderExtensions { get; }

            public void Dispose()
            {
                if (Directory.Exists(Environment.ExtensionsDirectory))
                {
                    Directory.Delete(Environment.ExtensionsDirectory, true);
                }
            }
        }

        protected ExtensionTestContext GetExtensionTestContext(string testName, string jsonConfig, bool includeNuGet = false)
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // test-packages artifact path.
            var relativePathToTestPackages = "../../../../../artifacts/testpackages";
            var relativePathToTestProjects = "../../../../../tests/projects";

            var effectiveRootFolder = Path.GetFullPath(relativePathToTestProjects, assemblyDirectory!);

            var packagesFullPath = Path.GetFullPath(relativePathToTestPackages, assemblyDirectory!);
            var nugetFileUri = new Uri("file://" + packagesFullPath);

            var configuration = new ConfigurationBuilder();

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfig));

            configuration.AddJsonStream(jsonStream);

            var config = configuration.Build();

            var sourceData = new SourceSettings(effectiveRootFolder);

            if (includeNuGet)
            {
                sourceData.AppendCustomSources(new[] { nugetFileUri.AbsoluteUri });
            }
            else
            {
                sourceData.ReplaceCustomSources(new[] { nugetFileUri.AbsoluteUri });
            }

            var packageInstallDirectory = Path.Combine(assemblyDirectory!, "testdirs", nameof(ExtensionResolveTests), testName);

            var extensions = config.GetSection("extensions").Get<PackageExtensionConfiguration[]>() ?? Enumerable.Empty<PackageExtensionConfiguration>();
            var folderExtensions = config.GetSection("localExtensions").Get<FolderExtensionConfiguration[]>() ?? Enumerable.Empty<FolderExtensionConfiguration>();

            return new ExtensionTestContext(effectiveRootFolder, packageInstallDirectory, config, sourceData, extensions, folderExtensions);
        }

        protected void AttachToDummyProject(ILoadedExtensions<IExtensionEntryPoint> set, IConfiguration config)
        {
            var proj = new Project();
            foreach (var entryPoint in set.ExtensionEntryPoints)
            {
                entryPoint.AttachToProject(config, proj);
            }
        }
    }
}
