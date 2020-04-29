using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Configuration;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Provides an <see cref="ISourceSettings"/> implementation that allows a custom package source list to
    /// be defined (potentially ignoring system configuration).
    /// </summary>
    public class SourceSettings : ISourceSettings
    {
        /// <inheritdoc/>
        public ISettings NuGetSettings { get; private set; }

        /// <inheritdoc/>
        public IPackageSourceProvider NugetSourceProvider { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceSettings"/> class.
        /// </summary>
        /// <param name="rootDir">The root directory in which to look for nuget settings.</param>
        public SourceSettings(string rootDir)
        {
            NuGetSettings = Settings.LoadDefaultSettings(rootDir);
            NugetSourceProvider = new PackageSourceProvider(NuGetSettings);
        }

        /// <summary>
        /// Append a custom set of package sources to the available configured defaults.
        /// </summary>
        /// <param name="sourceUrls">The set of custom sources.</param>
        public void AppendCustomSources(string[] sourceUrls)
        {
            NugetSourceProvider = new CustomPackageSourceProvider(NugetSourceProvider.LoadPackageSources().Concat(sourceUrls.Select(x => new PackageSource(x))));
        }

        /// <summary>
        /// Replace the set of default sources with a new set.
        /// </summary>
        /// <param name="sourceUrls">The set of source urls.</param>
        public void ReplaceCustomSources(string[] sourceUrls)
        {
            NugetSourceProvider = new CustomPackageSourceProvider(sourceUrls.Select(x => new PackageSource(x)));
        }

        /// <summary>
        /// Custom source to control the set of package sources.
        /// </summary>
        private class CustomPackageSourceProvider : IPackageSourceProvider
        {
            private List<PackageSource> sourceList;

            public string ActivePackageSourceName => sourceList.LastOrDefault().Name;

            public string DefaultPushSource => throw new NotImplementedException();

#pragma warning disable CS0067 // Not raising it, but required by the interface.
            public event EventHandler? PackageSourcesChanged;
#pragma warning restore CS0067

            public CustomPackageSourceProvider(IEnumerable<PackageSource> sources)
            {
                sourceList = new List<PackageSource>(sources);
            }

            public PackageSource GetPackageSourceByName(string name)
            {
                return sourceList.FirstOrDefault(x => x.Name == name);
            }

            public PackageSource GetPackageSourceBySource(string source)
            {
                return sourceList.FirstOrDefault(x => x.Source == source);
            }

            public bool IsPackageSourceEnabled(string name)
            {
                return GetPackageSourceByName(name)?.IsEnabled ?? false;
            }

            public IEnumerable<PackageSource> LoadPackageSources()
            {
                return sourceList;
            }

            public void AddPackageSource(PackageSource source)
            {
                throw new NotImplementedException();
            }

            public void DisablePackageSource(string name)
            {
                throw new NotImplementedException();
            }

            public void EnablePackageSource(string name)
            {
                throw new NotImplementedException();
            }

            public void RemovePackageSource(string name)
            {
                throw new NotImplementedException();
            }

            public void SaveActivePackageSource(PackageSource source)
            {
                throw new NotImplementedException();
            }

            public void SavePackageSources(IEnumerable<PackageSource> sources)
            {
                throw new NotImplementedException();
            }

            public void UpdatePackageSource(PackageSource source, bool updateCredentials, bool updateEnabled)
            {
                throw new NotImplementedException();
            }
        }
    }
}
