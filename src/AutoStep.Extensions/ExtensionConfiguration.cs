namespace AutoStep.Extensions
{
    /// <summary>
    /// Represents the configuration for a single extension to install.
    /// </summary>
    public class ExtensionConfiguration
    {
        /// <summary>
        /// Gets or sets the package ID.
        /// </summary>
        public string? Package { get; set; }

        /// <summary>
        /// Gets or sets an optional version range string.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether pre-release versions are permitted.
        /// </summary>
        public bool PreRelease { get; set; }
    }
}
