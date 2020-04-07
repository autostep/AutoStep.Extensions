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

        public string RootDir { get; }

        public ExtensionSourceSettings(string rootDir)
        {
            NuGetSettings = new Settings(rootDir);
            RootDir = rootDir;
        }

        public void UseNuGetConfiguration()
        {
            NuGetSettings = Settings.LoadDefaultSettings(RootDir);
        }

        public void UseCustomSources(string[] sources)
        {
            var packageSourceProvider = new PackageSourceProvider(Settings.LoadDefaultSettings(RootDir));

            packageSourceProvider.SavePackageSources(sources.Select(s => new PackageSource(s)));

            NuGetSettings = packageSourceProvider.Settings;
        }
    }
}
