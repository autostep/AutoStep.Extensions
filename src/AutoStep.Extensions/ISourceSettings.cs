using NuGet.Configuration;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Defines the interface for a provider of nuget source settings.
    /// </summary>
    public interface ISourceSettings
    {
        /// <summary>
        /// Gets the general shared nuget settings.
        /// </summary>
        public ISettings NuGetSettings { get; }

        /// <summary>
        /// Gets the package source provider.
        /// </summary>
        public IPackageSourceProvider SourceProvider { get; }
    }
}
