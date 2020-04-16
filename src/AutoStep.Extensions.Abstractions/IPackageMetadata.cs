namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines available metadata for a package.
    /// </summary>
    public interface IPackageMetadata
    {
        /// <summary>
        /// Gets the absolute path of the package's installed folder.
        /// </summary>
        public string PackageFolder { get; }

        /// <summary>
        /// Gets the package ID.
        /// </summary>
        public string PackageId { get; }

        /// <summary>
        /// Gets the installed package version.
        /// </summary>
        public string PackageVersion { get; }
    }
}
