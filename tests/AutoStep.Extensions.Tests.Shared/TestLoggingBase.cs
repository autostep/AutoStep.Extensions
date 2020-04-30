using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace AutoStep.Extensions.Tests.Shared
{
    public class TestLoggingBase
    {
        protected ILoggerFactory LogFactory { get; }

        public TestLoggingBase(ITestOutputHelper outputHelper)
        {
            LogFactory = TestLogFactory.Create(outputHelper);
        }
    }
}
