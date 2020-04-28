using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Graph;
using Microsoft.Build.Logging;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AutoStep.Extensions.Build
{
    class MsBuildLoadContext : IDisposable
    {
        private readonly string msBuildPath;
        private readonly IHostContext hostContext;
        private readonly ILogger logger;
        private readonly ProjectCollection projectCollection;
        private readonly ProjectGraph graph;
        //private readonly ProjectInstance containingProject;

        private MsBuildLoadContext(IEnumerable<string> projectPaths, string msBuildPath, IHostContext hostContext, ILogger logger, IDictionary<string, string> configOptions)
        {
            projectCollection = new ProjectCollection(configOptions);
            graph = new ProjectGraph(projectPaths, projectCollection);

            //var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //var containerProjectPath = Path.Combine(assemblyLocation, "Build", "containerproj.csproj");

            //containingProject = new ProjectInstance(containerProjectPath);

            //foreach (var entryPoint in graph.EntryPointNodes)
            //{
            //    containingProject.AddItem("ProjectReference", entryPoint.ProjectInstance.FullPath);
            //}

            this.msBuildPath = msBuildPath;
            this.hostContext = hostContext;
            this.logger = logger;
        }

        public static MsBuildLoadContext Create(IEnumerable<string> projectPaths, string msBuildPath, ILogger logger, IHostContext hostContext)
        {
            // Use MSBuild.
            var configOptions = new Dictionary<string, string>
            {
                { "Configuration", "Debug" },
            };

            return new MsBuildLoadContext(projectPaths, msBuildPath, hostContext, logger, configOptions);
        }

        public void Dispose()
        {
            projectCollection.Dispose();
        }

        public void PopulateDependencies(HashSet<PackageDependency> allDeps)
        {
            foreach (var projectNode in graph.ProjectNodes)
            {
                VisitProjectForDependencies(projectNode.ProjectInstance, allDeps);
            }
        }

        private void VisitProjectForDependencies(ProjectInstance project, HashSet<PackageDependency> dependencies)
        {
            var packages = project.GetItems("PackageReference");

            foreach (var package in packages)
            {
                var packageId = package.EvaluatedInclude;
                var packageVersion = package.GetMetadataValue("Version");
                var privateAssets = package.GetMetadataValue("PrivateAssets");

                if (privateAssets.Contains("all") || privateAssets.Contains("runtime"))
                {
                    // Don't include packages if they won't be copied to the output.
                    continue;
                }
                else
                {
                    PackageDependency dependency;

                    if (string.IsNullOrWhiteSpace(packageVersion))
                    {
                        dependency = new PackageDependency(packageId);
                    }
                    else
                    {
                        dependency = new PackageDependency(packageId, VersionRange.Parse(packageVersion));
                    }

                    if (!hostContext.DependencySuppliedByHost(dependency))
                    {
                        // Add it.
                        dependencies.Add(dependency);
                    }
                }
            }

            // Get the output directory.
            project.GetPropertyValue("TargetDir");
        }

        public async ValueTask<bool> Build()
        {
            var success = true;

            foreach (var rootNode in graph.GraphRoots)
            {
                var project = rootNode.ProjectInstance;

                var processRun = new ProcessRun(
                    "dotnet.exe",
                    msBuildPath,
                    "./MSBuild.dll",
                    "-t:restore,build",
                    "-v:minimal",
                    "\"" + project.FullPath + "\"");

                var exitCode = await processRun.Run();

                if (exitCode == 0)
                {
                    logger.LogDebug(processRun.Result);
                }
                else
                {
                    logger.LogError(processRun.Result);
                }
            }

            return success;

            //var buildLogger = new MsBuildLogWrapper(logger);

            //return containingProject.Build(new[] { buildLogger });
        }

        public IEnumerable<string> GetOutputBinaries()
        {
            var outputDirs = new List<string>();

            foreach (var entry in graph.ProjectNodes)
            {
                outputDirs.Add(entry.ProjectInstance.GetPropertyValue("TargetPath"));
            }

            return outputDirs;
        }

        private class MsBuildLogWrapper : Microsoft.Build.Framework.ILogger
        {
            private readonly ILogger logTarget;
            private readonly ConsoleLogger consoleLogger;
            private LogLevel activeLogLevel;

            public MsBuildLogWrapper(ILogger logTarget)
            {
                this.logTarget = logTarget;
                Verbosity = logTarget.IsEnabled(LogLevel.Trace) ? LoggerVerbosity.Detailed : LoggerVerbosity.Minimal;
                Parameters = string.Empty;
                consoleLogger = new ConsoleLogger(Verbosity, WriteToLogger, HandleColorSet, HandleColorReset);
                activeLogLevel = LogLevel.Debug;
            }

            public LoggerVerbosity Verbosity { get; set; }

            public string Parameters { get; set; }

            public void Initialize(IEventSource eventSource)
            {
                consoleLogger.Initialize(eventSource);
            }

            public void Shutdown()
            {
                consoleLogger.Shutdown();
            }

            private void HandleColorReset()
            {
                activeLogLevel = LogLevel.Debug;
            }

            private void HandleColorSet(ConsoleColor color)
            {
                if (color == ConsoleColor.Red)
                {
                    activeLogLevel = LogLevel.Error;
                }
                else if (color == ConsoleColor.Yellow)
                {
                    activeLogLevel = LogLevel.Warning;
                }
                else
                {
                    activeLogLevel = LogLevel.Debug;
                }
            }

            private void WriteToLogger(string message)
            {
                // Take the end newline off.
                logTarget.Log(activeLogLevel, message.TrimEnd('\r', '\n'));
            }
        }
    }
}
