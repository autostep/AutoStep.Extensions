using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Elements.Test;
using AutoStep.Execution;
using AutoStep.Execution.Contexts;
using AutoStep.Execution.Events;
using AutoStep.Extensions.Abstractions;
using AutoStep.Language;
using AutoStep.Language.Test;
using AutoStep.Projects;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AutoStep.Extensions.Tests
{
    public class ExtensionWithEventHandlerTests : BaseExtensionTests
    {
        public ExtensionWithEventHandlerTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public async Task ExtensionEventHandlerInvoked()
        {
            using var context = GetExtensionTestContext(nameof(ExtensionEventHandlerInvoked), @"
            {
                ""extensions"": [
                    { ""Package"" : ""TestExtensionWithEventHandler"" }
                ]
            }");

            var setLoader = new ExtensionSetLoader(context.RootDirectory, LogFactory, "autostep");

            using (var set = await setLoader.LoadExtensionsAsync<IExtensionEntryPoint>(
                context.Sources,
                context.Extensions,
                false,
                CancellationToken.None))
            {
                var file = new ProjectTestFile("/test", new StringContentSource(""));
                var builtFile = new FileElement
                {
                    Feature = new FeatureElement
                    {
                        Name = "My Feature",
                        Scenarios =
                        {
                            new ScenarioElement
                            {
                                Name = "My Scenario"
                            }
                        }
                    }
                };

                file.UpdateLastCompileResult(new FileCompilerResult(true, builtFile));
                file.UpdateLastLinkResult(new LinkResult(true, Enumerable.Empty<LanguageOperationMessage>(), null, builtFile));

                await DoExecuteTest(set, context, file);
            }
        }

        private async Task DoExecuteTest(ILoadedExtensions<IExtensionEntryPoint> extensions, ExtensionTestContext context, ProjectTestFile file)
        {
            var proj = new Project();

            proj.TryAddFile(file);

            foreach (var entryPoint in extensions.ExtensionEntryPoints)
            {
                entryPoint.AttachToProject(context.Configuration, proj);
            }

            var testRun = proj.CreateTestRun();

            var myHandler = new LocalCollectionHandler();

            testRun.Events.Add(myHandler);

            foreach (var entryPoint in extensions.ExtensionEntryPoints)
            {
                entryPoint.ExtendExecution(context.Configuration, testRun);
            }

            await testRun.ExecuteAsync();

            myHandler.Error.Should().NotBeNull();
        }

        private class LocalCollectionHandler : BaseEventHandler
        {
            public Exception? Error { get; set; }

            public override async ValueTask OnExecute(IServiceProvider scope, RunContext ctxt, Func<IServiceProvider, RunContext, ValueTask> nextHandler)
            {
                try
                {
                    await nextHandler(scope, ctxt);
                }
                catch (Exception ex)
                {
                    Error = ex;
                }
            }
        }
    }
}
