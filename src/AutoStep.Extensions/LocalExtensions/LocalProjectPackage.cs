using System.Collections.Generic;
using AutoStep.Extensions.LocalExtensions.Build;
using AutoStep.Projects;

namespace AutoStep.Extensions.LocalExtensions
{
    /// <summary>
    /// Exposes a local extension project as if it was a package, with a source directory to load the package from.
    /// </summary>
    internal class LocalProjectPackage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalProjectPackage"/> class.
        /// </summary>
        /// <param name="metadata">The project metadata.</param>
        /// <param name="watchMode">The watch mode for the project.</param>
        public LocalProjectPackage(ProjectMetadata metadata, PackageWatchMode watchMode)
        {
            ProjectFile = metadata.ProjectFile;
            ProjectDirectory = metadata.Directory;
            PackageId = metadata.PackageId;
            ProjectVersion = metadata.Version;
            BinaryDirectory = metadata.OutputDirectory;
            EntryPointDllName = metadata.OutputFileName;
            WatchMode = watchMode;
            SourceFiles = metadata.SourceFiles;
        }

        /// <summary>
        /// Gets the project file.
        /// </summary>
        public string ProjectFile { get; }

        /// <summary>
        /// Gets the directory of the local project.
        /// </summary>
        public string ProjectDirectory { get; }

        /// <summary>
        /// Gets the name of the project, treated as a package ID.
        /// </summary>
        public string PackageId { get; }

        /// <summary>
        /// Gets the project/package version.
        /// </summary>
        public string ProjectVersion { get; }

        /// <summary>
        /// Gets the binary directory, from which the output of the project build can be copied.
        /// </summary>
        public string BinaryDirectory { get; }

        /// <summary>
        /// Gets the name of the DLL that should be used as the entry point to the package.
        /// </summary>
        public string EntryPointDllName { get; }

        /// <summary>
        /// Gets the watch mode of the package.
        /// </summary>
        public PackageWatchMode WatchMode { get; }

        /// <summary>
        /// Gets the set of source files; will be empty if <see cref="WatchMode"/> is not equal to <see cref="PackageWatchMode.Full"/>.
        /// </summary>
        public IReadOnlyList<string> SourceFiles { get; }
    }
}
