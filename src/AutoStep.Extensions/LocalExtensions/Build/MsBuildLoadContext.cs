using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoStep.Extensions.LocalExtensions.Build;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AutoStep.Extensions.Build
{
    internal class MsBuildLoadContext : IDisposable
    {
        private readonly IHostContext hostContext;
        private readonly ILogger logger;
        private readonly ProjectCollection projectCollection;
        private readonly ProjectGraph graph;

        private MsBuildLoadContext(IEnumerable<string> projectPaths, IHostContext hostContext, ILogger logger, IDictionary<string, string> configOptions)
        {
            projectCollection = new ProjectCollection(configOptions);
            graph = new ProjectGraph(projectPaths, projectCollection);

            this.hostContext = hostContext;
            this.logger = logger;
        }

        public static MsBuildLoadContext Create(IEnumerable<string> projectPaths, ILogger logger, IHostContext hostContext)
        {
            // Use MSBuild.
            var configOptions = new Dictionary<string, string>
            {
                { "Configuration", "Debug" },
            };

            return new MsBuildLoadContext(projectPaths, hostContext, logger, configOptions);
        }

        public void Dispose()
        {
            projectCollection.Dispose();
        }

        public IEnumerable<PackageDependency> GetAllDependencies()
        {
            var allDeps = new HashSet<PackageDependency>(PackageDependencyComparer.Default);

            foreach (var projectNode in graph.ProjectNodes)
            {
                VisitProjectForDependencies(projectNode.ProjectInstance, allDeps);
            }

            return allDeps;
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
        }

        public IEnumerable<LocalProjectPackage> GetProjectsAsInstallablePackages()
        {
            foreach (var proj in graph.EntryPointNodes)
            {
                var projInstance = proj.ProjectInstance;

                var packageId = projInstance.GetPropertyValue("PackageId");

                if (string.IsNullOrEmpty(packageId))
                {
                    packageId = projInstance.GetPropertyValue("MSBuildProjectName");
                }

                var version = projInstance.GetPropertyValue("Version");

                if (string.IsNullOrEmpty(version))
                {
                    // Default it.
                    version = "1.0.0";
                }

                var outputDirectory = projInstance.GetPropertyValue("TargetDir");
                var outputDllName = projInstance.GetPropertyValue("AssemblyName") + ".dll";

                yield return new LocalProjectPackage(packageId, version, outputDirectory, outputDllName);
            }
        }

        public async ValueTask<bool> Build()
        {
            var success = true;

            foreach (var rootNode in graph.GraphRoots)
            {
                var project = rootNode.ProjectInstance;

                var processRun = new ProcessRun(
                    "dotnet",
                    project.Directory,
                    "build",
                    "-v minimal");

                var exitCode = await processRun.Run();

                if (exitCode == 0)
                {
                    logger.LogDebug(processRun.Result);
                }
                else
                {
                    success = false;
                    logger.LogError(processRun.Result);
                }
            }

            return success;
        }
    }
}
