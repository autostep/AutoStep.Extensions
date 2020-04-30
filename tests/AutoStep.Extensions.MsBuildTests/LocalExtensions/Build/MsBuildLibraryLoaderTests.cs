using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using System.Text;
using AutoStep.Extensions.LocalExtensions.Build;
using AutoStep.Projects;
using FluentAssertions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Xunit;

namespace AutoStep.Extensions.Tests.LocalExtensions.Build
{
    public class MsBuildLibraryLoaderTests
    {
        [Fact]
        public void LoaderEnablesMsBuildTypeUsage()
        {
            Action act = () => new MsBuildConsumingType();

            // Fails before.
            act.Should().Throw<FileNotFoundException>();
            
            MsBuildLibraryLoader.EnsureLoaded();

            // Succeeds after.
            var consumer2 = new MsBuildConsumingType();
            consumer2.AssertPopulated();

            // Can call again.
            MsBuildLibraryLoader.EnsureLoaded();
            MsBuildLibraryLoader.EnsureLoaded();
        }

        private class MsBuildConsumingType
        {
            private ProjectCollection collection;

            public MsBuildConsumingType()
            {
                collection = new ProjectCollection();
            }

            public void AssertPopulated()
            {
                collection.Should().NotBeNull();
            }
        }
    }
}
