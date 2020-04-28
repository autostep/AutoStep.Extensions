using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.Tests
{
    public class LocalExtensionTests : BaseExtensionTests
    {
        public LocalExtensionTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public async Task CanLoadLocalExtensionWithDependency()
        {
            using var context = GetExtensionTestContext(nameof(CanLoadLocalExtensionWithDependency), @"
            {
                ""localExtensions"": [
                    { ""folder"" : ""LocalExtension"", ""Name"": ""LocalExtension"" }
                ]
            }");

            //var setLoader = new ExtensionSetLoader(context.RootDirectory, context.FolderExtensionDir, LogFactory, "autostep");

            //using (var set = await setLoader.ResolveExtensionsAsync<IExtensionEntryPoint>(
            //    context.Sources,
            //    context.Extensions,
            //    context.FolderExtensions,
            //    false,
            //    CancellationToken.None))
            //{
            //}
        }
    }
}
