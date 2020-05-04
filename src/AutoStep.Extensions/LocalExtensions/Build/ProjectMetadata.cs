using System.Collections.Generic;

namespace AutoStep.Extensions.LocalExtensions.Build
{
    /// <summary>
    /// Defines the metadata for a single project.
    /// </summary>
    internal struct ProjectMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectMetadata"/> struct.
        /// </summary>
        /// <param name="projectFile">The absolute project file path.</param>
        /// <param name="directory">The absolute project directory.</param>
        /// <param name="packageId">The package ID.</param>
        /// <param name="version">The package version.</param>
        /// <param name="outputDirectory">The output directory.</param>
        /// <param name="outputFileName">The output file name.</param>
        /// <param name="sourceFiles">The set of source files.</param>
        public ProjectMetadata(string projectFile, string directory, string packageId, string version, string outputDirectory, string outputFileName, IReadOnlyList<string> sourceFiles)
        {
            ProjectFile = projectFile;
            Directory = directory;
            PackageId = packageId;
            Version = version;
            OutputDirectory = outputDirectory;
            OutputFileName = outputFileName;
            SourceFiles = sourceFiles;
        }

        /// <summary>
        /// Gets the absolute project file path.
        /// </summary>
        public string ProjectFile { get; }

        /// <summary>
        /// Gets the absolute project directory.
        /// </summary>
        public string Directory { get; }

        /// <summary>
        /// Gets the package ID.
        /// </summary>
        public string PackageId { get; }

        /// <summary>
        /// Gets the package version.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the output directory.
        /// </summary>
        public string OutputDirectory { get; }

        /// <summary>
        /// Gets the output file name.
        /// </summary>
        public string OutputFileName { get; }

        /// <summary>
        /// Gets the set of source files.
        /// </summary>
        public IReadOnlyList<string> SourceFiles { get; }
    }
}
