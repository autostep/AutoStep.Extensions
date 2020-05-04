namespace AutoStep.Extensions
{
    /// <summary>
    /// Represents the configuration for a local folder extension.
    /// </summary>
    public class FolderExtensionConfiguration
    {
        /// <summary>
        /// Gets or sets the folder; can be relative to the project directory, or absolute.
        /// </summary>
        public string? Folder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the files within each project should be watched for change, and MSBuild
        /// invoked when loading the extension.
        /// </summary>
        public bool Watch { get; set; }
    }
}
