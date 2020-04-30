using System;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Assertion;
using AutoStep.Execution.Contexts;
using AutoStep.Execution.Events;

namespace TestExtensionWithEventHandler
{
    internal class MyHandler : BaseEventHandler
    {
        public override ValueTask OnExecuteAsync(IServiceProvider scope, RunContext ctxt, Func<IServiceProvider, RunContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            throw new AssertionException("My handler invoked");
        }
    }
}
