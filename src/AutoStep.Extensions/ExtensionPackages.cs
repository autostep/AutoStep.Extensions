using System.Collections.Generic;
using System.IO;

namespace AutoStep.Extensions
{
    internal class ExtensionPackages
    {
        public ExtensionPackages(string extensionsRootDir, IReadOnlyList<PackageEntry> packages)
        {
            ExtensionsRootDir = extensionsRootDir;
            Packages = packages;
        }

        public string ExtensionsRootDir { get; }

        public IReadOnlyList<PackageEntry> Packages { get; }
    }
}
