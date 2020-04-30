using Microsoft.Build.Locator;

namespace AutoStep.Extensions.LocalExtensions.Build
{
    /// <summary>
    /// Provides singleton access to the MSBuildLocator.
    /// </summary>
    internal static class MsBuildLibraryLoader
    {
        private static readonly object Sync = new object();
        private static bool isLoaded;

        /// <summary>
        /// Ensures that the MSBuild libraries have been loaded.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (!isLoaded)
            {
                lock (Sync)
                {
                    if (!isLoaded)
                    {
                        isLoaded = true;
                        MSBuildLocator.RegisterDefaults();
                    }
                }
            }
        }
    }
}
