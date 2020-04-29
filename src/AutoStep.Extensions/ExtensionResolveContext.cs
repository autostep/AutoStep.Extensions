using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;

namespace AutoStep.Extensions
{
    internal class ExtensionResolveContext
    {
        public ExtensionResolveContext(IEnumerable<PackageExtensionConfiguration> packageExtensions, IEnumerable<FolderExtensionConfiguration> folderExtensions)
        {
            PackageExtensions = packageExtensions;
            FolderExtensions = folderExtensions;
            AdditionalPackagesRequired = new List<PackageDependency>();
        }

        public IEnumerable<PackageExtensionConfiguration> PackageExtensions { get; }

        public IEnumerable<FolderExtensionConfiguration> FolderExtensions { get; }

        public List<PackageDependency> AdditionalPackagesRequired { get; }
    }
}
