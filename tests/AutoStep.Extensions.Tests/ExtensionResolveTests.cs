using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Projects;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.Tests
{
    public class ExtensionResolveTests : BaseExtensionTests
    {
        public ExtensionResolveTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
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

                File.Exists(set.GetPackagePath("TestExtension1", "lib", "netstandard2.1", "TestExtension1.dll")).Should().BeTrue();
            }
        }

        [Fact]
        public async Task ExtensionWithAnother()
        {
            using var context = GetExtensionTestContext(nameof(ExtensionWithAnother), @"
            {
                ""extensions"": [
                    { ""Package"" : ""TestExtensionReferencesNewtonSoft"" }
                ]
            }", includeNuGet: true);

            using (var set = await ExtensionSetLoader.LoadExtensionsAsync(
                context.RootDirectory,
                Assembly.GetExecutingAssembly(),
                context.Sources,
                LogFactory,
                context.Configuration,
                CancellationToken.None))
            {
                set.LoadedPackages.Should().Contain(p => p.PackageId == "Newtonsoft.Json");

                set.AttachToProject(context.Configuration, new Project());
            }
        }
    }
}
