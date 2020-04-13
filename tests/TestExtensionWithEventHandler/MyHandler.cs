using System;
using System.Threading.Tasks;
using AutoStep.Assertion;
using AutoStep.Execution.Contexts;
using AutoStep.Execution.Events;

namespace TestExtensionWithEventHandler
{
    internal class MyHandler : BaseEventHandler
    {
        public override ValueTask OnExecute(IServiceProvider scope, RunContext ctxt, Func<IServiceProvider, RunContext, ValueTask> nextHandler)
        {
            throw new AssertionException("My handler invoked");
        }
    }
}
