using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Configuration;

namespace AutoStep.Extensions
{
    public class ExtensionSourceSettings
    {
        internal ISettings NuGetSettings { get; private set; }

        internal IPackageSourceProvider SourceProvider { get; private set; }

        public string RootDir { get; }

        public ExtensionSourceSettings(string rootDir)
        {
            NuGetSettings = Settings.LoadDefaultSettings(rootDir);
            SourceProvider = new PackageSourceProvider(NuGetSettings);
            RootDir = rootDir;
        }

        public void AppendCustomSources(string[] sources)
        {
            SourceProvider = new CustomPackageSourceProvider(SourceProvider.LoadPackageSources().Concat(sources.Select(x => new PackageSource(x))));
        }

        public void ReplaceCustomSources(string[] sources)
        {
            SourceProvider = new CustomPackageSourceProvider(sources.Select(x => new PackageSource(x)));
        }

        private class CustomPackageSourceProvider : IPackageSourceProvider
        {
            private List<PackageSource> sourceList;

            public string ActivePackageSourceName => sourceList.LastOrDefault().Name;

            public string DefaultPushSource => throw new NotImplementedException();

            public event EventHandler PackageSourcesChanged;

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
