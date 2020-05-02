namespace AutoStep.Extensions
{
    /// <summary>
    /// Constant values for the <see cref="IPackageMetadata.DependencyType"/> field.
    /// </summary>
    public static class PackageDependencyTypes
    {
        /// <summary>
        /// A 'root' or 'top-level' package, from an extension configuration.
        /// </summary>
        public const string ExtensionPackage = "rootPackage";

        /// <summary>
        /// An additional root package, added by a local extension or other additional resolver.
        /// </summary>
        public const string AdditionalRootPackage = "additionalRoot";

        /// <summary>
        /// A dependency of one of the root packages.
        /// </summary>
        public const string Dependency = "package";
    }
}
