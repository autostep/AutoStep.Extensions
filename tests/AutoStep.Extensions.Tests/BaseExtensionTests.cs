using System;
using System.Diagnostics;
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
            public string RootDirectory { get; set; }

            public IConfiguration Configuration { get; set; }
            public ExtensionSourceSettings Sources { get; internal set; }

            public void Dispose()
            {
                // Don't clean up if the debugger is attached. Want to be able to look at the output.
                if (Directory.Exists(RootDirectory) && !Debugger.IsAttached)
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

            var fullPath = Path.GetFullPath(relativePath, assemblyDirectory);

            var nugetFileUri = new Uri("file://" + fullPath);

            var testRootDirectory = Path.Combine(assemblyDirectory, "testdirs", nameof(ExtensionResolveTests), testName);

            var configuration = new ConfigurationBuilder();

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonConfig));

            configuration.AddJsonStream(jsonStream);

            var config = configuration.Build();

            var sourceData = new ExtensionSourceSettings(testRootDirectory);

            if (includeNuGet)
            {
                sourceData.AppendCustomSources(new[] { nugetFileUri.AbsoluteUri });
            }
            else
            {
                sourceData.ReplaceCustomSources(new[] { nugetFileUri.AbsoluteUri });
            }

            return new ExtensionTestContext
            {
                RootDirectory = testRootDirectory,
                Configuration = config,
                Sources = sourceData
            };
        }

    }
}
