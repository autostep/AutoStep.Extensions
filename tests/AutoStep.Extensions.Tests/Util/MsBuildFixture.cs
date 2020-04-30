using System;
using System.Collections.Generic;
using System.Text;
using AutoStep.Extensions.LocalExtensions.Build;

namespace AutoStep.Extensions.Tests
{
    public class MsBuildFixture : IDisposable
    {
        public MsBuildFixture()
        {
            // Make sure our MSBuild deps are loaded.
            MsBuildLibraryLoader.EnsureLoaded();
        }

        public void Dispose()
        {
            // Nothing to do here.
        }
    }
}
