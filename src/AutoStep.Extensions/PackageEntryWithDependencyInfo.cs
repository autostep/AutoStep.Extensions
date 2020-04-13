using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using System.Collections.Generic;

namespace AutoStep.Extensions
{
    internal class PackageEntryWithDependencyInfo : PackageEntry
    {
        public PackageEntryWithDependencyInfo(
            SourcePackageDependencyInfo packageDepInfo,
            string packageFolder,
            string? entryPoint,
            IEnumerable<string> libFiles,
            IEnumerable<string> contentFiles)
            : base(packageDepInfo.Id, packageDepInfo.Version.ToNormalizedString(), packageFolder, entryPoint, libFiles, contentFiles, packageDepInfo.Dependencies)
        {
        }
    }
}
