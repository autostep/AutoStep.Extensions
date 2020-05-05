using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.IntegrationTests.Utils;
using AutoStep.Extensions.Watch;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.IntegrationTests
{
    [Collection("BuildNoParallel")]
    public class WatcherTests : BaseExtensionTests
    {
        public WatcherTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        public enum Watch
        {
            Default,
            Full
        }

        public enum AssertMode
        {
            PackageStillFine,
            PackageBecomesDirty,
        }

        public enum Build
        {
            Release,
            Debug
        }

        [Theory]
        [InlineData(Watch.Default, Build.Release, "LocalExtension.csproj", AssertMode.PackageBecomesDirty)]        
        [InlineData(Watch.Default, Build.Release, "bin/Release/netstandard2.1/LocalExtension.dll", AssertMode.PackageBecomesDirty)]
        [InlineData(Watch.Default, Build.Release, "bin/Release/netstandard2.1/content.asi", AssertMode.PackageBecomesDirty)]
        [InlineData(Watch.Default, Build.Debug, "bin/Debug/netstandard2.1/LocalExtension.dll", AssertMode.PackageBecomesDirty)]
        [InlineData(Watch.Default, Build.Debug, "bin/Debug/netstandard2.1/content.asi", AssertMode.PackageBecomesDirty)]
        [InlineData(Watch.Default, Build.Release, "TestFile1.cs", AssertMode.PackageStillFine)]
        [InlineData(Watch.Full, Build.Release, "LocalExtension.csproj", AssertMode.PackageBecomesDirty)]
        [InlineData(Watch.Full, Build.Release, "TestFile1.cs", AssertMode.PackageBecomesDirty)]
        [InlineData(Watch.Full, Build.Release, "SubFolder/EmbeddedFile.txt", AssertMode.PackageBecomesDirty)]
        [InlineData(Watch.Full, Build.Release, "content.asi", AssertMode.PackageBecomesDirty)]
        [InlineData(Watch.Full, Build.Release, "contentnocopy.asi", AssertMode.PackageStillFine)]
        public async Task CanWatch(Watch watch, Build buildMode, string relativePathToModify, AssertMode assertMode)
        {
            using var context = GetExtensionTestContext(nameof(CanWatch), @$"
            {{
                ""localExtensions"": [
                    {{ ""folder"": ""LocalExtension"", ""watch"": {(watch == Watch.Full).ToString().ToLower()} }}
                ]
            }}", includeNuGet: true);

            var setLoader = new ExtensionSetLoader(context.RootDirectory, context.PackageInstallDirectory, LogFactory, "autostep");

            var resolvedPackages = await setLoader.ResolveExtensionsAsync(
                context.Sources,
                context.Extensions,
                context.FolderExtensions,
                false,
                buildMode == Build.Debug,
                CancellationToken.None);

            resolvedPackages.IsValid.Should().BeTrue();

            var installedSet = await resolvedPackages.InstallAsync(CancellationToken.None);

            using var watcher = new PackageCollectionWatcher();

            watcher.SyncPackages(installedSet.Packages.OfType<ILocalExtensionPackageMetadata>());

            TaskCompletionSource<ILocalExtensionPackageMetadata> taskSource = new TaskCompletionSource<ILocalExtensionPackageMetadata>();

            watcher.OnWatchedPackageDirty += (sender, package) =>
            {
                taskSource.SetResult(package);
            };

            // Start the watch.
            watcher.Start();

            var localMeta = installedSet.Packages.OfType<ILocalExtensionPackageMetadata>().First();

            var fullFilePath = Path.GetFullPath(relativePathToModify, localMeta.SourceProjectFolder);

            // 'Touch' the file.
            var projectFile = new FileInfo(fullFilePath);
            projectFile.LastWriteTimeUtc = DateTime.UtcNow;

            if (assertMode == AssertMode.PackageBecomesDirty)
            {
                taskSource.Awaiting(t => t.Task).Should().CompleteWithin(TimeSpan.FromSeconds(1));
                taskSource.Task.Result.PackageId.Should().Be("AutoStep.Extensions.LocalExtension");
            }
            else
            {
                // Task should not have completed after 100ms.
                await Task.Delay(100);
                taskSource.Task.IsCompleted.Should().BeFalse();
            }
        }
    }
}
