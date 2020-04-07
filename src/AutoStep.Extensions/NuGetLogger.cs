using Microsoft.Extensions.Logging;
using NuGet.Common;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using LogLevel = Microsoft.Extensions.Logging.LogLevel;
    using NuGetLogLevel = NuGet.Common.LogLevel;

    internal class NuGetLogger : LoggerBase, NuGet.Common.ILogger
    {
        private readonly ILogger logger;

        public NuGetLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public override void Log(ILogMessage message)
        {
            var level = message.Level switch
            {
                NuGetLogLevel.Error => LogLevel.Error,
                NuGetLogLevel.Warning => LogLevel.Warning,
                NuGetLogLevel.Information => LogLevel.Information,
                _ => LogLevel.Debug
            };

            logger.Log(level, message.FormatWithCode());
        }

        public override Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }
    }
}
