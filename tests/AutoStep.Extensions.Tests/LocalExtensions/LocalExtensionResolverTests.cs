using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.Tests.Shared;
using FluentAssertions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.Tests.LocalExtensions
{    
    [Collection("Build")]
    public class LocalExtensionResolverTests : TestLoggingBase
    {
        private string testProjectsFolder = Path.GetFullPath("../../../../projects/", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);

        public LocalExtensionResolverTests(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        [Fact]
        public async Task NoFolderExtensionsReturnsValidEmptySet()
        {
            var hostContext = new Mock<IHostContext>().Object;

            var localResolver = new LocalExtensionResolver(hostContext, LogFactory.CreateLogger("test"));

            // Give it a bad array of packages (because it shouldn't check them). No folder extensions.
            var resolved = await localResolver.ResolvePackagesAsync(new ExtensionResolveContext(new PackageExtensionConfiguration[1], 
                                                                                                Enumerable.Empty<FolderExtensionConfiguration>()), CancellationToken.None);

            resolved.IsValid.Should().BeTrue();
            resolved.PackageIds.Should().BeEmpty();
            resolved.Exception.Should().BeNull();
        }


        [Fact]
        public async Task LocatesExtensionProjectInFolderAbsolutePath()
        {
            string testProjectFolder = Path.Combine(testProjectsFolder, "LocalExtension");
    
            var hostContext = new Mock<IHostContext>().Object;

            var localResolver = new LocalExtensionResolver(hostContext, LogFactory.CreateLogger("test"));

            var folderExtension = new FolderExtensionConfiguration { Folder = testProjectFolder };

            var resolveContext = new ExtensionResolveContext(Enumerable.Empty<PackageExtensionConfiguration>(),
                                                             new[] { folderExtension });

            var resolved = await localResolver.ResolvePackagesAsync(resolveContext, CancellationToken.None);

            resolved.IsValid.Should().BeTrue();
            resolved.PackageIds.Should().Equal("AutoStep.Extensions.LocalExtension");
            resolved.Exception.Should().BeNull();
        }

        [Fact]
        public async Task LocatesExtensionProjectInFolderRelativePath()
        {
            var hostContext = new Mock<IHostContext>();
            hostContext.Setup(x => x.RootDirectory).Returns(testProjectsFolder);

            var localResolver = new LocalExtensionResolver(hostContext.Object, LogFactory.CreateLogger("test"));

            var folderExtension = new FolderExtensionConfiguration { Folder = "LocalExtension" };

            var resolveContext = new ExtensionResolveContext(Enumerable.Empty<PackageExtensionConfiguration>(),
                                                             new[] { folderExtension });

            var resolved = await localResolver.ResolvePackagesAsync(resolveContext, CancellationToken.None);

            resolved.IsValid.Should().BeTrue();
            resolved.PackageIds.Should().Equal("AutoStep.Extensions.LocalExtension");
            resolved.Exception.Should().BeNull();
        }

        [Fact]
        public async Task NonExistentProjectFolderError()
        {
            var hostContext = new Mock<IHostContext>();
            hostContext.Setup(x => x.RootDirectory).Returns(testProjectsFolder);

            var localResolver = new LocalExtensionResolver(hostContext.Object, LogFactory.CreateLogger("test"));

            var folderExtension = new FolderExtensionConfiguration { Folder = "NotAFolder" };

            var resolveContext = new ExtensionResolveContext(Enumerable.Empty<PackageExtensionConfiguration>(),
                                                             new[] { folderExtension });

            var resolved = await localResolver.ResolvePackagesAsync(resolveContext, CancellationToken.None);

            resolved.IsValid.Should().BeFalse();
            resolved.Exception.Should().BeNull();
        }

        [Fact]
        public async Task FolderWithNoProject()
        {
            var hostContext = new Mock<IHostContext>();

            var localResolver = new LocalExtensionResolver(hostContext.Object, LogFactory.CreateLogger("test"));

            var folderExtension = new FolderExtensionConfiguration { Folder = testProjectsFolder };

            var resolveContext = new ExtensionResolveContext(Enumerable.Empty<PackageExtensionConfiguration>(),
                                                                new[] { folderExtension });

            var resolved = await localResolver.ResolvePackagesAsync(resolveContext, CancellationToken.None);

            resolved.IsValid.Should().BeFalse();
            resolved.Exception.Should().BeNull();
        }

        [Fact]
        public async Task LoadsVbExtensionProject()
        {
            var hostContext = new Mock<IHostContext>();
            hostContext.Setup(x => x.RootDirectory).Returns(testProjectsFolder);

            var localResolver = new LocalExtensionResolver(hostContext.Object, LogFactory.CreateLogger("test"));

            var folderExtension = new FolderExtensionConfiguration { Folder = "VbLocalExtension" };

            var resolveContext = new ExtensionResolveContext(Enumerable.Empty<PackageExtensionConfiguration>(),
                                                             new[] { folderExtension });

            var resolved = await localResolver.ResolvePackagesAsync(resolveContext, CancellationToken.None);

            resolved.IsValid.Should().BeTrue();
            resolved.PackageIds.Should().Equal("VbLocalExtension");
            resolved.Exception.Should().BeNull();
        }

        [Fact]
        public async Task LoadsFSharpExtensionProject()
        {
            var hostContext = new Mock<IHostContext>();
            hostContext.Setup(x => x.RootDirectory).Returns(testProjectsFolder);

            var localResolver = new LocalExtensionResolver(hostContext.Object, LogFactory.CreateLogger("test"));


            var folderExtension = new FolderExtensionConfiguration { Folder = "FsLocalExtension" };

            var resolveContext = new ExtensionResolveContext(Enumerable.Empty<PackageExtensionConfiguration>(),
                                                             new[] { folderExtension });

            var resolved = await localResolver.ResolvePackagesAsync(resolveContext, CancellationToken.None);

            resolved.IsValid.Should().BeTrue();
            resolved.PackageIds.Should().Equal("FsLocalExtension");
            resolved.Exception.Should().BeNull();
        }

        [Fact]
        public async Task LoadsMultipleExtensionProjects()
        {
            var hostContext = new Mock<IHostContext>();
            hostContext.Setup(x => x.RootDirectory).Returns(testProjectsFolder);

            var localResolver = new LocalExtensionResolver(hostContext.Object, LogFactory.CreateLogger("test"));


            var resolveContext = new ExtensionResolveContext(
                Enumerable.Empty<PackageExtensionConfiguration>(),
                new[] { 
                    new FolderExtensionConfiguration { Folder = "LocalExtension" },
                    new FolderExtensionConfiguration { Folder = "VbLocalExtension" },
                    new FolderExtensionConfiguration { Folder = "FsLocalExtension" }
                });

            var resolved = await localResolver.ResolvePackagesAsync(resolveContext, CancellationToken.None);

            resolved.IsValid.Should().BeTrue();
            resolved.PackageIds.Should().Equal("AutoStep.Extensions.LocalExtension", "VbLocalExtension", "FsLocalExtension");
            resolved.Exception.Should().BeNull();
        }
    }
}
