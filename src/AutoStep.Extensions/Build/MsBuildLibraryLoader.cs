using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoStep.Extensions.Build
{
    public class MsBuildLibraryLoader
    {
        private static readonly object sync = new object();
        private static bool isLoaded;
        private static VisualStudioInstance vsInstance;

        public static string GetMsBuildPath()
        {
            if (!isLoaded)
            {
                lock (sync)
                {
                    if (!isLoaded)
                    {
                        vsInstance = MSBuildLocator.RegisterDefaults();
                    }
                }
            }

            return vsInstance.MSBuildPath;
        }
    }
}
