using System;
using System.Collections.Generic;
using System.Text;
using AutoStep.Extensions.Watch;
using FluentAssertions;
using Moq;
using Xunit;

namespace AutoStep.Extensions.Tests.Watch
{
    public class PackageCollectionWatcherTests
    {
        [Fact]
        public void SyncPackagesCreatesNewPackageWatchers()
        {
            var invokeCount = 0;

            var collectionWatcher = new PackageCollectionWatcher(meta => {

                invokeCount++;
                return new Mock<IPackageWatcher>().Object;
            });

            var mockPackage1 = new Mock<ILocalExtensionPackageMetadata>();
            mockPackage1.SetupGet(x => x.SourceProjectFolder).Returns("folder1");
            mockPackage1.SetupGet(x => x.WatchMode).Returns(PackageWatchMode.Full);

            var mockPackage2 = new Mock<ILocalExtensionPackageMetadata>();
            mockPackage2.SetupGet(x => x.SourceProjectFolder).Returns("folder2");
            mockPackage2.SetupGet(x => x.WatchMode).Returns(PackageWatchMode.Full);

            collectionWatcher.SyncPackages(new[]
            {
                mockPackage1.Object, mockPackage2.Object
            });

            invokeCount.Should().Be(2);
        }

        [Fact]
        public void SyncPackagesIgnoresPackagesWithNoWatch()
        {
            var invokeCount = 0;

            var collectionWatcher = new PackageCollectionWatcher(meta => {

                invokeCount++;
                return new Mock<IPackageWatcher>().Object;
            });

            var mockPackage1 = new Mock<ILocalExtensionPackageMetadata>();
            mockPackage1.SetupGet(x => x.SourceProjectFolder).Returns("folder1");
            mockPackage1.SetupGet(x => x.WatchMode).Returns(PackageWatchMode.None);

            var mockPackage2 = new Mock<ILocalExtensionPackageMetadata>();
            mockPackage2.SetupGet(x => x.SourceProjectFolder).Returns("folder2");
            mockPackage2.SetupGet(x => x.WatchMode).Returns(PackageWatchMode.Full);

            collectionWatcher.SyncPackages(new[]
            {
                mockPackage1.Object, mockPackage2.Object
            });

            invokeCount.Should().Be(1);
        }

        [Fact]
        public void SyncPackagesUpdatesExistingPackageWatchers()
        {
            var mockWatchers = new List<Mock<IPackageWatcher>>();

            var collectionWatcher = new PackageCollectionWatcher(meta => {

                var mock = new Mock<IPackageWatcher>();
                mockWatchers.Add(mock);
                return mock.Object;
            });

            var mockPackage1 = new Mock<ILocalExtensionPackageMetadata>();
            mockPackage1.SetupGet(x => x.SourceProjectFolder).Returns("folder1");
            mockPackage1.SetupGet(x => x.WatchMode).Returns(PackageWatchMode.Full);

            var mockPackage2 = new Mock<ILocalExtensionPackageMetadata>();
            mockPackage2.SetupGet(x => x.SourceProjectFolder).Returns("folder2");
            mockPackage2.SetupGet(x => x.WatchMode).Returns(PackageWatchMode.Full);

            collectionWatcher.SyncPackages(new[]
            {
                mockPackage1.Object, mockPackage2.Object
            });

            mockWatchers.Should().HaveCount(2);

            collectionWatcher.SyncPackages(new[]
            {
                mockPackage1.Object, mockPackage2.Object
            });

            // Verify that update was invoked on both.
            mockWatchers[0].Verify(x => x.Update(mockPackage1.Object), Times.Once);
            mockWatchers[1].Verify(x => x.Update(mockPackage2.Object), Times.Once);
        }
    }
}
