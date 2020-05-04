namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines the possible package watch modes.
    /// </summary>
    public enum PackageWatchMode
    {
        /// <summary>
        /// No watch.
        /// </summary>
        None,

        /// <summary>
        /// Only watch the project output for changes.
        /// </summary>
        OutputOnly,

        /// <summary>
        /// Watch project source for changes so we can initiate a re-build.
        /// </summary>
        Full,
    }
}
