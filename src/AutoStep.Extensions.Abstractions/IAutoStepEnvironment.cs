namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines an interface for accessing properties of the AutoStep environment.
    /// </summary>
    public interface IAutoStepEnvironment
    {
        /// <summary>
        /// Gets the project root directory.
        /// </summary>
        string RootDirectory { get; }

        /// <summary>
        /// Gets the folder that extensions are installed in.
        /// </summary>
        string ExtensionsDirectory { get; }
    }
}
