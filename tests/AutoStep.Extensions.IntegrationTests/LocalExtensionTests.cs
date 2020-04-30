using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.IntegrationTests
{
    public class LocalExtensionTests : BaseExtensionTests
    {
        public LocalExtensionTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public async Task CanLoadLocalExtensionWithDependency()
        {
            using var context = GetExtensionTestContext(nameof(CanLoadLocalExtensionWithDependency), @"
            {
                ""localExtensions"": [
                    { ""folder"": ""LocalExtension"" }
                ]
            }", includeNuGet: true);

            var setLoader = new ExtensionSetLoader(context.RootDirectory, context.PackageInstallDirectory, LogFactory, "autostep");

            var resolvedPackages = await setLoader.ResolveExtensionsAsync(
                context.Sources,
                context.Extensions,
                context.FolderExtensions,
                false,
                CancellationToken.None);

            resolvedPackages.IsValid.Should().BeTrue();

            var installedSet = await resolvedPackages.InstallAsync(CancellationToken.None);

            using (var loadedExtensions = installedSet.LoadExtensionsFromPackages<IExtensionEntryPoint>(LogFactory))
            {
                loadedExtensions.Packages.Should().HaveCount(4);
                loadedExtensions.Packages.Select(x => x.PackageId).Should().Contain("Newtonsoft.Json", "Serilog", "LocalExtension", "AutoMapper");

                loadedExtensions.ExtensionEntryPoints.Should().HaveCount(1);

                AttachToDummyProject(loadedExtensions, context.Configuration);
            }
        }
    }
}
