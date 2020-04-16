using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.Abstractions;
using AutoStep.Projects;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NuGet.Packaging.Signing;
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
        public async Task SimplePackageLoad()
        {
            using var context = GetExtensionTestContext(nameof(SimplePackageLoad), @"
            {
                ""extensions"": [
                    { ""Package"" : ""TestExtension1"", ""prerelease"": true }
                ]
            }");

            var setLoader = new ExtensionSetLoader(context.RootDirectory, LogFactory, "autostep");

            using (var set = await setLoader.LoadExtensionsAsync<IExtensionEntryPoint>(
                context.Sources,
                context.Extensions,
                false,
                CancellationToken.None))
            {
                set.LoadedPackages.Should().HaveCount(1);
                set.LoadedPackages.First().PackageId.Should().Be("TestExtension1");
                set.LoadedPackages.First().PackageVersion.Should().Be("1.0.0-alpha-1");

                File.Exists(set.GetPackagePath("TestExtension1", "lib", "netstandard2.1", "TestExtension1.dll")).Should().BeTrue();
            }
        }

        [Fact]
        public async Task ExtensionWithDependency()
        {
            using var context = GetExtensionTestContext(nameof(ExtensionWithDependency), @"
            {
                ""extensions"": [
                    { ""Package"" : ""TestExtensionReferencesNewtonSoft"", ""prerelease"": true }
                ]
            }", includeNuGet: true);

            var setLoader = new ExtensionSetLoader(context.RootDirectory, LogFactory, "autostep");

            using (var set = await setLoader.LoadExtensionsAsync<IExtensionEntryPoint>(
                context.Sources,
                context.Extensions,
                false,
                CancellationToken.None))
            {
                set.LoadedPackages.Should().HaveCount(2);
                set.LoadedPackages.Should().Contain(p => p.PackageId == "Newtonsoft.Json");

                AttachToDummyProject(set, context.Configuration);
            }
        }

        [Fact]
        public async Task CacheCanBeUsedOnSubsequentLoads()
        {
            using var context = GetExtensionTestContext(nameof(SimplePackageLoad), @"
            {
                ""extensions"": [
                    { ""Package"" : ""TestExtension1"", ""prerelease"": true }
                ]
            }");

            var setLoader = new ExtensionSetLoader(context.RootDirectory, LogFactory, "autostep");

            using (var set = await setLoader.LoadExtensionsAsync<IExtensionEntryPoint>(
                context.Sources,
                context.Extensions,
                false,
                CancellationToken.None))
            {
                set.LoadedPackages.Should().HaveCount(1);
                set.LoadedPackages.First().PackageId.Should().Be("TestExtension1");
                set.LoadedPackages.First().PackageVersion.Should().Be("1.0.0-alpha-1");

                File.Exists(set.GetPackagePath("TestExtension1", "lib", "netstandard2.1", "TestExtension1.dll")).Should().BeTrue();
            }

            // Second load doesn't need any nuget sources; strict empty mock means it will throw if anything
            // tries to access the nuget sources lists.
            using (var set = await setLoader.LoadExtensionsAsync<IExtensionEntryPoint>(
                new Mock<ISourceSettings>(MockBehavior.Strict).Object,
                context.Extensions,
                false,
                CancellationToken.None))
            {
                set.LoadedPackages.Should().HaveCount(1);
                set.LoadedPackages.First().PackageId.Should().Be("TestExtension1");
                set.LoadedPackages.First().PackageVersion.Should().Be("1.0.0-alpha-1");

                File.Exists(set.GetPackagePath("TestExtension1", "lib", "netstandard2.1", "TestExtension1.dll")).Should().BeTrue();
            }
        }


        private void AttachToDummyProject(ILoadedExtensions<IExtensionEntryPoint> set, IConfiguration config)
        {
            var proj = new Project();
            foreach (var entryPoint in set.ExtensionEntryPoints)
            {
                entryPoint.AttachToProject(config, proj);
            }
        }
    }
}
