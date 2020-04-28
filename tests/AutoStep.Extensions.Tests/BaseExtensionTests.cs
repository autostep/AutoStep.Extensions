using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoStep.Extensions.NuGetExtensions;
using AutoStep.Extensions.Tests.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace AutoStep.Extensions.Tests
{
    public abstract class BaseExtensionTests
    {
        protected ILoggerFactory LogFactory { get; }

        public BaseExtensionTests(ITestOutputHelper outputHelper)
        {
            LogFactory = TestLogFactory.Create(outputHelper);
        }

        protected class ExtensionTestContext : IDisposable
        {
            public ExtensionTestContext(
                string rootDirectory,
                string folderExtensionBaseDirectory,
                IConfiguration configuration,
                ISourceSettings sources,
                IEnumerable<PackageExtensionConfiguration> extensions,
                IEnumerable<FolderExtensionConfiguration> folderExtensions)
            {
                RootDirectory = rootDirectory;
                FolderExtensionDir = folderExtensionBaseDirectory;
                Configuration = configuration;
                Sources = sources;
                Extensions = extensions;
                FolderExtensions = folderExtensions;
            }

            public string RootDirectory { get; }

            public IConfiguration Configuration { get; }

            public ISourceSettings Sources { get;  }

            public IEnumerable<PackageExtensionConfiguration> Extensions { get; }

            public IEnumerable<FolderExtensionConfiguration> FolderExtensions { get; }

            public string FolderExtensionDir { get; internal set; }

            public void Dispose()
            {
                if (Directory.Exists(RootDirectory))
                {
                    Directory.Delete(RootDirectory, true);
                }
            }
        }

        protected ExtensionTestContext GetExtensionTestContext(string testName, string jsonConfig, bool includeNuGet = false)
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // test-packages artifact path.
            var relativePathToTestPackages = "../../../../../artifacts/testpackages";
            var relativePathToTestProjects = "../../../../../tests";

            var packagesFullPath = Path.GetFullPath(relativePathToTestPackages, assemblyDirectory!);
            var testProjectsFullPath = Path.GetFullPath(relativePathToTestProjects, assemblyDirectory!);

            var nugetFileUri = new Uri("file://" + packagesFullPath);

            var testRootDirectory = Path.Combine(assemblyDirectory!, "testdirs", nameof(ExtensionResolveTests), testName);

            var configuration = new ConfigurationBuilder();

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfig));

            configuration.AddJsonStream(jsonStream);

            var config = configuration.Build();

            var sourceData = new SourceSettings(testRootDirectory);

            if (includeNuGet)
            {
                sourceData.AppendCustomSources(new[] { nugetFileUri.AbsoluteUri });
            }
            else
            {
                sourceData.ReplaceCustomSources(new[] { nugetFileUri.AbsoluteUri });
            }

            var extensions = config.GetSection("extensions").Get<PackageExtensionConfiguration[]>() ?? Enumerable.Empty<PackageExtensionConfiguration>();
            var folderExtensions = config.GetSection("localExtensions").Get<FolderExtensionConfiguration[]>() ?? Enumerable.Empty<FolderExtensionConfiguration>();

            return new ExtensionTestContext(testRootDirectory, testProjectsFullPath, config, sourceData, extensions, folderExtensions);
        }

    }
}
