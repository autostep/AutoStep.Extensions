namespace AutoStep.Extensions
{
    /// <summary>
    /// Constant values for the 'type' field for each runtime library. Used to identify root packages.
    /// </summary>
    internal static class ExtensionRuntimeLibraryTypes
    {
        /// <summary>
        /// A 'root' or 'top-level' package, from an extension configuration.
        /// </summary>
        public const string RootPackage = "rootPackage";

        /// <summary>
        /// A dependency of one of the root packages.
        /// </summary>
        public const string Dependency = "package";
    }
}
