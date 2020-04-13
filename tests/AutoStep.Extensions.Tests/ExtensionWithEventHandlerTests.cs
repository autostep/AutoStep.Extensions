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

            using (var set = await ExtensionSetLoader.LoadExtensionsAsync(
                context.RootDirectory,
                context.Sources,
                LogFactory,
                context.Configuration,
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

        private async Task DoExecuteTest(IExtensionSet extensions, ExtensionTestContext context, ProjectTestFile file)
        {
            var proj = new Project();

            proj.TryAddFile(file);

            extensions.AttachToProject(context.Configuration, proj);

            var testRun = proj.CreateTestRun();

            var myHandler = new LocalCollectionHandler();

            testRun.Events.Add(myHandler);

            extensions.ExtendExecution(context.Configuration, testRun);

            await testRun.ExecuteAsync();

            myHandler.Error.Should().NotBeNull();
        }

        private class LocalCollectionHandler : BaseEventHandler
        {
            public Exception Error { get; set; }

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
