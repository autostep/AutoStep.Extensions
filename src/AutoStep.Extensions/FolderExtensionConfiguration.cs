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
    }
}
