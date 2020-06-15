using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using AutoStep.Assertion;
using AutoStep.Execution.Contexts;
using AutoStep.Execution.Events;

namespace TestExtensionWithEventHandler
{
    internal class MyHandler : BaseEventHandler
    {
        public override ValueTask OnExecuteAsync(ILifetimeScope scope, RunContext ctxt, Func<ILifetimeScope, RunContext, CancellationToken, ValueTask> nextHandler, CancellationToken cancelToken)
        {
            throw new AssertionException("My handler invoked");
        }
    }
}
