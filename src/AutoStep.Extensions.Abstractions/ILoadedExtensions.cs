using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStep.Extensions.Abstractions
{
    public interface ILoadedExtensions
    {
        string ExtensionsRootDir { get; }

        IEnumerable<IPackageMetadata> LoadedPackages { get; }

        string GetPackagePath(string packageId, params string[] directoryParts);
    }
}
