﻿using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.IntegrationTests.Utils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.IntegrationTests
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
                loadedExtensions.Packages.Should().HaveCount(1);
                loadedExtensions.Packages.First().PackageId.Should().Be("TestExtension1");
                loadedExtensions.Packages.First().PackageVersion.Should().Be("1.0.0-alpha.1");

                File.Exists(loadedExtensions.GetPackagePath("TestExtension1", "lib", "netstandard2.1", "TestExtension1.dll")).Should().BeTrue();
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
            }", ContextOptions.IncludeNuget);

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
                loadedExtensions.Packages.Should().HaveCount(2);
                loadedExtensions.Packages.Should().Contain(p => p.PackageId == "Newtonsoft.Json");

                loadedExtensions.ExtensionEntryPoints.Should().HaveCount(1);

                AttachToDummyProject(loadedExtensions, context.Configuration);
            }
        }

        [Fact]
        public async Task CanDetermineBestVersionOfExtensionWhenPresentInMultipleSources()
        {
            using var context = GetExtensionTestContext(nameof(SimplePackageLoad), @"
            {
                ""extensions"": [
                    { ""Package"" : ""TestExtension1"", ""prerelease"": true }
                ]
            }", ContextOptions.IncludeSecondaryLocalPackageSource);

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
                loadedExtensions.Packages.Should().HaveCount(1);
                loadedExtensions.Packages.First().PackageId.Should().Be("TestExtension1");
                loadedExtensions.Packages.First().PackageVersion.Should().Be("1.0.0-alpha.10");

                File.Exists(loadedExtensions.GetPackagePath("TestExtension1", "lib", "netstandard2.1", "TestExtension1.dll")).Should().BeTrue();
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
                loadedExtensions.Packages.Should().HaveCount(1);
                loadedExtensions.Packages.First().PackageId.Should().Be("TestExtension1");
                loadedExtensions.Packages.First().PackageVersion.Should().Be("1.0.0-alpha.1");

                File.Exists(loadedExtensions.GetPackagePath("TestExtension1", "lib", "netstandard2.1", "TestExtension1.dll")).Should().BeTrue();
            }

            // Second load doesn't need any nuget sources; strict empty mock means it will throw if anything
            // tries to access the nuget sources lists.
            resolvedPackages = await setLoader.ResolveExtensionsAsync(
                context.Sources,
                context.Extensions,
                context.FolderExtensions,
                false,
                false,
                CancellationToken.None);

            resolvedPackages.IsValid.Should().BeTrue();

            installedSet = await resolvedPackages.InstallAsync(CancellationToken.None);

            using (var loadedExtensions = installedSet.LoadExtensionsFromPackages<IExtensionEntryPoint>(LogFactory))
            {
                loadedExtensions.Packages.Should().HaveCount(1);
                loadedExtensions.Packages.First().PackageId.Should().Be("TestExtension1");
                loadedExtensions.Packages.First().PackageVersion.Should().Be("1.0.0-alpha.1");

                File.Exists(loadedExtensions.GetPackagePath("TestExtension1", "lib", "netstandard2.1", "TestExtension1.dll")).Should().BeTrue();
            }
        }
    }
}
