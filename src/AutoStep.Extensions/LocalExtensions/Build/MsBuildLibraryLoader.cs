using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoStep.Extensions.LocalExtensions.Build
{
    public class MsBuildLibraryLoader
    {
        private static readonly object sync = new object();
        private static bool isLoaded;

        public static void EnsureLoaded()
        {
            if (!isLoaded)
            {
                lock (sync)
                {
                    if (!isLoaded)
                    {
                        MSBuildLocator.RegisterDefaults();
                    }
                }
            }
        }
    }
}
