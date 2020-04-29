using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AutoStep.Extensions.Build
{
    /// <summary>
    /// Executes a command line tool.
    /// </summary>
    internal class ProcessRun
    {
        private readonly string exeName;
        private readonly string workingDirectory;
        private readonly string[] args;

        private StringBuilder Output { get; set; } = new StringBuilder();

        /// <summary>
        /// Get the STDOUT result of the command line process.
        /// </summary>
        public string Result => Output.ToString();

        private Dictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Create a new command line executor.
        /// </summary>
        /// <param name="exeName">The name/path of the exe to run.</param>
        /// <param name="workingDirectory"></param>
        /// <param name="args">The command line arguments.</param>
        public ProcessRun(string exeName, string workingDirectory, params string[] args)
        {
            this.exeName = exeName;
            this.workingDirectory = workingDirectory;
            this.args = args;
        }

        /// <summary>
        /// Run the tool, returning a wait-able task where the result contains the contents of STDOUT.
        /// </summary>
        public async Task<int> Run()
        {
            using var proc = new Process
            {
                StartInfo =
                {
                    FileName = exeName,
                    WorkingDirectory = workingDirectory,
                    Arguments = string.Join(" ", args),
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                },
            };

            foreach (var item in EnvironmentVariables)
            {
                proc.StartInfo.EnvironmentVariables.Add(item.Key, item.Value);
            }

            proc.OutputDataReceived += (sender, eventArgs) =>
            {
                // Lock access to the output buffer in case there is STDOUT and STDERR at the same time.
                lock (Output)
                {
                    Output.Append(eventArgs.Data);
                }
            };

            proc.ErrorDataReceived += (sender, eventArgs) =>
            {
                lock (Output)
                {
                    Output.Append(eventArgs.Data);
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

                await processExitSource.Task;

                return proc.ExitCode;
            }
            catch (Exception ex)
            {
                Output.AppendLine($"Failed to launch process: {ex.Message}");

                return 1;
            }
        }
    }
}
