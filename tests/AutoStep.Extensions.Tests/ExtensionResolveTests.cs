using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.Tests.Utils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.Tests
{
    public class ExtensionResolveTests
    {
        protected ILoggerFactory LogFactory { get; }

        public ExtensionResolveTests(ITestOutputHelper outputHelper)
        {
            LogFactory = TestLogFactory.Create(outputHelper);
        }

        private class ExtensionTestContext : IDisposable
        {
            public string RootDirectory { get; set; }

            public IConfiguration Configuration { get; set;  }
            public ExtensionSourceSettings Sources { get; internal set; }

            public void Dispose()
            {
                // Don't clean up if the debugger is attached. Want to be able to look at the output.
                if (Directory.Exists(RootDirectory) && !Debugger.IsAttached)
                {
                    int failCount = 0;
                    while(failCount < 50)
                    {
                        // We want to try and clean up the directory after we've unloaded the context, but it may take a moment
                        // for the runtime to unload.
                        try
                        {
                            Directory.Delete(RootDirectory, true);
                            return;
                        } 
                        catch
                        {
                            failCount++;
                            Thread.Sleep(1);
                        }
                    }
                }
            }
        }

        private ExtensionTestContext GetExtensionTestContext(string testName, string jsonConfig)
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
            sourceData.ReplaceCustomSources(new[] { nugetFileUri.AbsoluteUri });

            return new ExtensionTestContext
            {
                RootDirectory = testRootDirectory,
                Configuration = config,
                Sources = sourceData
            };
        }

        [Fact]
        public async Task FullEndToEnd()
        {
            using var context = GetExtensionTestContext(nameof(FullEndToEnd), @"
            {
                ""extensions"": [
                    { ""Package"" : ""TestExtension1"" }
                ]
            }");

            using (var set = await ExtensionSetLoader.LoadExtensionsAsync(
                context.RootDirectory,
                Assembly.GetExecutingAssembly(),
                context.Sources,
                LogFactory,
                context.Configuration,
                CancellationToken.None))
            {
                set.LoadedPackages.Should().HaveCount(1);
                set.LoadedPackages.First().PackageId.Should().Be("TestExtension1");
                set.LoadedPackages.First().PackageVersion.Should().Be("1.0.0");
            }
        }
    }
}
