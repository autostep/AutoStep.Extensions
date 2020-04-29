using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NuGet.Common;

namespace AutoStep.Extensions.NuGetExtensions
{
    using ILogger = Microsoft.Extensions.Logging.ILogger;
    using LogLevel = Microsoft.Extensions.Logging.LogLevel;
    using NuGetLogLevel = NuGet.Common.LogLevel;

    /// <summary>
    /// Provides a nuget logger that writes to a normal .NET ILogger.
    /// </summary>
    internal class NuGetLogger : LoggerBase, NuGet.Common.ILogger
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        public NuGetLogger(ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public override void Log(ILogMessage message)
        {
            var level = message.Level switch
            {
                NuGetLogLevel.Error => LogLevel.Error,
                NuGetLogLevel.Warning => LogLevel.Warning,
                _ => LogLevel.Debug
            };

            logger.Log(level, message.FormatWithCode());
        }

        /// <inheritdoc/>
        public override Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }
    }
}
