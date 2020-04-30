using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace AutoStep.Extensions.Tests.Util
{
    [CollectionDefinition("Build")]
    public class BuildCollection : ICollectionFixture<MsBuildFixture>
    {
    }
}
