using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace AutoStep.Extensions.LocalExtensions.Build
{
    /// <summary>
    /// Executes a command line program and captures the output.
    /// </summary>
    internal class ProcessRun
    {
        private readonly string exeName;
        private readonly string workingDirectory;
        private readonly string[] args;
        private readonly StringBuilder outputBuilder;

        /// <summary>
        /// Gets the output of the command line process.
        /// </summary>
        public string Output => outputBuilder.ToString();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessRun"/> class.
        /// </summary>
        /// <param name="exeName">The name/path of the exe to run.</param>
        /// <param name="workingDirectory">The working directory for the process.</param>
        /// <param name="args">The set of arguments to the process.</param>
        public ProcessRun(string exeName, string workingDirectory, params string[] args)
        {
            this.exeName = exeName;
            this.workingDirectory = workingDirectory;
            this.args = args;
            outputBuilder = new StringBuilder();
        }

        /// <summary>
        /// Run the tool, returning a wait-able task where the result contains the contents of STDOUT.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation. Result of the task is the exit code of the process.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Want to treat all exceptions from Process.Start as an exit code of 1, with custom message content.")]
        public async Task<int> Run()
        {
            var startInfo = new ProcessStartInfo(exeName)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
            };

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var proc = new Process
            {
                StartInfo = startInfo,
            };

            proc.OutputDataReceived += (sender, eventArgs) =>
            {
                // Lock access to the output buffer in case there is STDOUT and STDERR at the same time.
                lock (outputBuilder)
                {
                    outputBuilder.Append(eventArgs.Data);
                }
            };

            proc.ErrorDataReceived += (sender, eventArgs) =>
            {
                lock (outputBuilder)
                {
                    outputBuilder.Append(eventArgs.Data);
                }
            };

            var processExitSource = new TaskCompletionSource<bool>();

            proc.Exited += (sender, eventArgs) =>
            {
                processExitSource.TrySetResult(true);
            };

            proc.EnableRaisingEvents = true;

            try
            {
                proc.Start();

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                if (proc.HasExited)
                {
                    return proc.ExitCode;
                }

                await processExitSource.Task.ConfigureAwait(false);

                return proc.ExitCode;
            }
            catch (Exception ex)
            {
                outputBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture, BuildMessages.FailedToLaunchProcess, ex.Message));

                return 1;
            }
        }
    }
}
