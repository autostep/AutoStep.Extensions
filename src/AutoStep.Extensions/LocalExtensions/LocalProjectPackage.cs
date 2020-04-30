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
        /// <param name="projectName">The name of the project, treated as a package ID.</param>
        /// <param name="projectVersion">The project/package version.</param>
        /// <param name="binaryDirectory">The binary directory, from which the output of the project build can be copied.</param>
        /// <param name="entryPointDllName">The name of the DLL that should be used as the entry point to the package.</param>
        public LocalProjectPackage(string projectName, string projectVersion, string binaryDirectory, string entryPointDllName)
        {
            ProjectName = projectName;
            ProjectVersion = projectVersion;
            BinaryDirectory = binaryDirectory;
            EntryPointDllName = entryPointDllName;
        }

        /// <summary>
        /// Gets the name of the project, treated as a package ID.
        /// </summary>
        public string ProjectName { get; }

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
    }
}
