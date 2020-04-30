using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AutoStep.Extensions.Tests.Util
{
    [CollectionDefinition("Build", DisableParallelization = true)]
    public class BuildCollection : ICollectionFixture<MsBuildFixture>
    {
    }
}
