using System.IO;
using System.Linq;
using System.Reflection;
using AutoStep.Extensions.LocalExtensions.Build;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NuGet.Packaging.Core;
using Xunit;

namespace AutoStep.Extensions.Tests.LocalExtensions.Build
{
    /// <summary>
    /// Note: Testing of MsBuildProjectCollection.Build is done via the integration tests, rather than here.
    /// </summary>
    [Collection("Build")]
    public class MsBuildProjectCollectionTests
    {
        private string TestProjectFile => Path.GetFullPath("../../../../projects/LocalExtension/LocalExtension.csproj", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

        [Fact]
        public void CanGetAllDependenciesOfProject()
        {
            var mockHostContext = new Mock<IHostContext>(MockBehavior.Strict);
            mockHostContext.Setup(x => x.DependencySuppliedByHost(It.IsAny<PackageDependency>())).Returns(false);

            var projectCollection = MsBuildProjectCollection.Create(new[] { TestProjectFile }, NullLogger.Instance, mockHostContext.Object);

            var deps = projectCollection.GetAllDependencies();

            deps.Should().HaveCount(5);
            deps.Should().Contain(p => p.Id == "Serilog" && p.VersionRange.ToString() == "[2.9.0, )");
            deps.Should().Contain(p => p.Id == "AutoMapper" && p.VersionRange.ToString() == "[9.0.0, )");
            deps.Should().Contain(p => p.Id == "Newtonsoft.Json" && p.VersionRange.ToString() == "[12.0.1, 13.0.0)");
            deps.Should().Contain(p => p.Id == "AutoStep.Extensions.Abstractions");
            deps.Should().Contain(p => p.Id == "AutoStep");
        }

        [Fact]
        public void DependenciesExcludesPrivatePackageReferences()
        {
            var mockHostContext = new Mock<IHostContext>(MockBehavior.Strict);
            mockHostContext.Setup(x => x.DependencySuppliedByHost(It.IsAny<PackageDependency>())).Returns(false);

            var projectCollection = MsBuildProjectCollection.Create(new[] { TestProjectFile }, NullLogger.Instance, mockHostContext.Object);

            var deps = projectCollection.GetAllDependencies();

            deps.Should().NotContain(p => p.Id == "Microsoft.CodeAnalysis.FxCopAnalyzers");
        }

        [Fact]
        public void DependenciesExcludesPackagesWithExcludedRuntime()
        {
            var mockHostContext = new Mock<IHostContext>(MockBehavior.Strict);
            mockHostContext.Setup(x => x.DependencySuppliedByHost(It.IsAny<PackageDependency>())).Returns(false);

            var projectCollection = MsBuildProjectCollection.Create(new[] { TestProjectFile }, NullLogger.Instance, mockHostContext.Object);

            var deps = projectCollection.GetAllDependencies();

            deps.Should().NotContain(p => p.Id == "StyleCop.Analyzers");
        }

        [Fact]
        public void HostContextCanFilterDependencies()
        {
            var mockHostContext = new Mock<IHostContext>(MockBehavior.Strict);
            mockHostContext.Setup(x => x.DependencySuppliedByHost(It.IsAny<PackageDependency>())).Returns<PackageDependency>(dep =>
            {
                if (dep.Id == "AutoStep" || dep.Id == "AutoStep.Extensions.Abstractions")
                {
                    return true;
                }

                return false;
            });

            var projectCollection = MsBuildProjectCollection.Create(new[] { TestProjectFile }, NullLogger.Instance, mockHostContext.Object);

            var deps = projectCollection.GetAllDependencies();

            deps.Should().HaveCount(3);
            deps.Should().NotContain(p => p.Id == "AutoStep.Extensions.Abstractions");
            deps.Should().NotContain(p => p.Id == "AutoStep");
        }

        [Fact]
        public void GetProjectsAsInstallablePackagesReturnsCorrectDetails()
        {
            var projectCollection = MsBuildProjectCollection.Create(new[] { TestProjectFile }, NullLogger.Instance, new Mock<IHostContext>().Object);

            var packages = projectCollection.GetProjectsAsInstallablePackages();

            packages.Should().HaveCount(1);

            var package = packages.First();

            package.ProjectName.Should().Be("AutoStep.Extensions.LocalExtension");
            package.ProjectVersion.Should().Be("1.0.0-custom.1");
            package.EntryPointDllName.Should().Be("LocalExtension.dll");

            var projectFolder = Path.GetDirectoryName(TestProjectFile);
            var expectedDirectory = Path.Combine(projectFolder!, "bin", "Release", "netstandard2.1") + Path.DirectorySeparatorChar;

            package.BinaryDirectory.Should().Be(expectedDirectory);
        }

        [Fact]
        public void GetProjectsAsInstallablePackagesReturnsDebugDirectoryWhenDebugModeEnabled()
        {
            var projectCollection = MsBuildProjectCollection.Create(new[] { TestProjectFile }, NullLogger.Instance, new Mock<IHostContext>().Object, debugMode: true);

            var packages = projectCollection.GetProjectsAsInstallablePackages();

            packages.Should().HaveCount(1);

            var package = packages.First();

            package.ProjectName.Should().Be("AutoStep.Extensions.LocalExtension");
            package.ProjectVersion.Should().Be("1.0.0-custom.1");
            package.EntryPointDllName.Should().Be("LocalExtension.dll");

            var projectFolder = Path.GetDirectoryName(TestProjectFile);
            var expectedDirectory = Path.Combine(projectFolder!, "bin", "Debug", "netstandard2.1") + Path.DirectorySeparatorChar;

            package.BinaryDirectory.Should().Be(expectedDirectory);
        }
    }
}
