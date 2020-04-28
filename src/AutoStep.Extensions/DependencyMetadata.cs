using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging.Core;

namespace AutoStep.Extensions
{    
    internal class DependencyMetadata : IPackageDependency
    {
        public DependencyMetadata(string packageId, string packageVersion)
        {
            PackageId = packageId;
            PackageVersion = packageVersion;
        }

        public string PackageId { get; }

        public string PackageVersion { get; }
    }
}
