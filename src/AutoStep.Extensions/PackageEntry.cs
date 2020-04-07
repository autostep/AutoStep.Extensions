using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging.Core;

namespace AutoStep.Extensions
{
    public interface IPackageMetadata
    {
        public string PackageFolder { get; }

        public string PackageId { get; }

        public string PackageVersion { get; }
    }

    internal class PackageEntry : IPackageMetadata
    {
        public PackageEntry(
            string packageId,
            string packageVersion,
            string packageFolder,
            string entryPoint,
            IEnumerable<string> libFiles,
            IEnumerable<string> contentFiles,
            IEnumerable<PackageDependency>? dependencies = null)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
            PackageFolder = packageFolder;
            LibFiles = libFiles.ToList();
            ContentFiles = contentFiles.ToList();
            EntryPoint = entryPoint;
            Dependencies = dependencies ?? Enumerable.Empty<PackageDependency>();
        }

        public string PackageFolder { get; }

        public string PackageId { get; }

        public string PackageVersion { get; }

        public IReadOnlyList<string> LibFiles { get; }

        public IReadOnlyList<string> ContentFiles { get; }

        public IEnumerable<PackageDependency> Dependencies { get; }

        public string EntryPoint { get; }
    }
}
