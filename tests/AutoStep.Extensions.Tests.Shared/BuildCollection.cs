using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AutoStep.Extensions.Tests.Shared
{
    [CollectionDefinition("BuildNoParallel", DisableParallelization = true)]
    public class BuildCollection : ICollectionFixture<MsBuildFixture>
    {
    }

    [CollectionDefinition("Build")]
    public class BuildCollectionParallel : ICollectionFixture<MsBuildFixture>
    {
    }
}
