using System;
using Microsoft.Extensions.Logging;

namespace AutoStep.Extensions.Tests.Utils
{
    public class TestLogger : ILogger
    {
        private readonly TestLogProvider provider;
        private readonly string name;

        public TestLogger(TestLogProvider provider, string name)
        {
            this.provider = provider;
            this.name = name;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            provider.Log(name, logLevel, eventId, state, exception, formatter);
        }
    }
}
