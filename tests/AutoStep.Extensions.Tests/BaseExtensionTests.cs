using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
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
            public ExtensionTestContext(string rootDirectory, IConfiguration configuration, ISourceSettings sources, IEnumerable<ExtensionConfiguration> extensions)
            {
                RootDirectory = rootDirectory;
                Configuration = configuration;
                Sources = sources;
                Extensions = extensions;
            }

            public string RootDirectory { get; }

            public IConfiguration Configuration { get; }

            public ISourceSettings Sources { get;  }

            public IEnumerable<ExtensionConfiguration> Extensions { get; }

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
            var relativePath = "../../../../../artifacts/testpackages";

            var fullPath = Path.GetFullPath(relativePath, assemblyDirectory!);

            var nugetFileUri = new Uri("file://" + fullPath);

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

            var extensions = config.GetSection("extensions").Get<ExtensionConfiguration[]>();

            return new ExtensionTestContext(testRootDirectory, config, sourceData, extensions);
        }

    }
}
