namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines additional metadata for local extensions.
    /// </summary>
    public interface ILocalExtensionPackageMetadata : IPackageMetadata
    {
        /// <summary>
        /// Gets the absolute path to the folder the project was loaded from.
        /// </summary>
        string SourceProjectFolder { get; }

        /// <summary>
        /// Gets the absolute path to the binary directory that the package was copied from.
        /// </summary>
        string SourceBinaryDirectory { get; }
    }
}
