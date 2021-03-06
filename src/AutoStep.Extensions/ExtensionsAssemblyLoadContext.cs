﻿using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Custom assembly load context.
    /// </summary>
    internal class ExtensionsAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly InstalledExtensionPackages extFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtensionsAssemblyLoadContext"/> class.
        /// </summary>
        /// <param name="extFiles">The set of packages that contain files loadable by this context.</param>
        public ExtensionsAssemblyLoadContext(InstalledExtensionPackages extFiles)
            : base(true)
        {
            this.extFiles = extFiles;
        }

        /// <inheritdoc/>
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var dllName = assemblyName.Name + ".dll";

            // Find the first DLL in the set of packages.
            foreach (var package in extFiles.Packages)
            {
                var matchingFile = package.LibFiles.FirstOrDefault(f => Path.GetFileName(f) == dllName);

                if (matchingFile is object)
                {
                    return LoadFromAssemblyPathWithoutLock(Path.GetFullPath(matchingFile, package.PackageFolder));
                }
            }

            return null;
        }

        /// <summary>
        /// Load an assembly into the context from a given path, without taking a lock on the file.
        /// </summary>
        /// <param name="path">The absolute assembly path.</param>
        /// <returns>A loaded assembly.</returns>
        public Assembly LoadFromAssemblyPathWithoutLock(string path)
        {
            // Load the assembly into memory and load from that, rather than loading from a given path.
            // We don't want to take a lock on the file on-disk.
            using (var assemblyStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return LoadFromStream(assemblyStream);
            }
        }
    }
}
