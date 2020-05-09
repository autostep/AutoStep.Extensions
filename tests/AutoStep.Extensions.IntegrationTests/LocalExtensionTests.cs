using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.IntegrationTests
{
    [Collection("BuildNoParallel")]
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

            var setLoader = new ExtensionSetLoader(context.Environment, LogFactory, "autostep");

            var resolvedPackages = await setLoader.ResolveExtensionsAsync(
                context.Sources,
                context.Extensions,
                context.FolderExtensions,
                false,
                false,
                CancellationToken.None);

            resolvedPackages.IsValid.Should().BeTrue();

            var installedSet = await resolvedPackages.InstallAsync(CancellationToken.None);

            using (var loadedExtensions = installedSet.LoadExtensionsFromPackages<IExtensionEntryPoint>(LogFactory))
            {
                loadedExtensions.Packages.Should().HaveCount(4);
                loadedExtensions.Packages.Select(x => x.PackageId).Should().Contain("Newtonsoft.Json", "Serilog", "AutoStep.Extensions.LocalExtension", "AutoMapper");

                loadedExtensions.ExtensionEntryPoints.Should().HaveCount(1);

                AttachToDummyProject(loadedExtensions, context.Configuration);
            }
        }

        [Fact]
        public async Task PreviouslyLoadedExtensionPackageRemovedFromInstallLocationAutomatically()
        {
            using var context1 = GetExtensionTestContext(nameof(PreviouslyLoadedExtensionPackageRemovedFromInstallLocationAutomatically), @"
            {
                ""localExtensions"": [
                    { ""folder"": ""LocalExtension"" }
                ]
            }", includeNuGet: true);

            var setLoader = new ExtensionSetLoader(context1.Environment, LogFactory, "autostep");

            var resolvedPackages = await setLoader.ResolveExtensionsAsync(
                context1.Sources,
                context1.Extensions,
                context1.FolderExtensions,
                false,
                false,
                CancellationToken.None);

            resolvedPackages.IsValid.Should().BeTrue();

            var installedSet1 = await resolvedPackages.InstallAsync(CancellationToken.None);

            var path = installedSet1.Packages.First(x => x.PackageId == "AutoStep.Extensions.LocalExtension").PackageFolder;

            Directory.Exists(path).Should().BeTrue();

            // Now do it again, but without any extensions.
            using var context2 = GetExtensionTestContext(nameof(PreviouslyLoadedExtensionPackageRemovedFromInstallLocationAutomatically), @"
            {
            }", includeNuGet: true);

            resolvedPackages = await setLoader.ResolveExtensionsAsync(
                context2.Sources,
                context2.Extensions,
                context2.FolderExtensions,
                false,
                false,
                CancellationToken.None);

            resolvedPackages.IsValid.Should().BeTrue();

            var installedSet2 = await resolvedPackages.InstallAsync(CancellationToken.None);

            installedSet2.Packages.Should().BeEmpty();

            // The same path should no longer exist.
            Directory.Exists(path).Should().BeFalse();

            // In fact, there should be no directories in the folder.
            Directory.GetDirectories(context1.Environment.ExtensionsDirectory).Should().BeEmpty();
        }
    }
}
