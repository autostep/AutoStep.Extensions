using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Graph;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AutoStep.Extensions.LocalExtensions.Build
{
    /// <summary>
    /// Contains a set of loaded MSBuild projects, that can be interrogated and built.
    /// </summary>
    internal class MsBuildProjectCollection : IDisposable
    {
        private readonly IHostContext hostContext;
        private readonly ILogger logger;
        private readonly ProjectCollection projectCollection;
        private readonly ProjectGraph graph;

        private MsBuildProjectCollection(IEnumerable<string> projectPaths, IHostContext hostContext, ILogger logger, IDictionary<string, string> configOptions)
        {
            // Define a project collection, and construct the graph of projects.
            projectCollection = new ProjectCollection(configOptions);
            graph = new ProjectGraph(projectPaths, configOptions, projectCollection);

            this.hostContext = hostContext;
            this.logger = logger;
        }

        /// <summary>
        /// Create a new project collection, given a set of project paths.
        /// </summary>
        /// <param name="projectPaths">The set of absolute project paths.</param>
        /// <param name="logger">A logger for the collection.</param>
        /// <param name="hostContext">The host context.</param>
        /// <param name="debugMode">Whether to use the Debug project configuration.</param>
        /// <returns>A new project collection.</returns>
        public static MsBuildProjectCollection Create(IEnumerable<string> projectPaths, ILogger logger, IHostContext hostContext, bool debugMode = false)
        {
            // Use MSBuild.
            var configOptions = new Dictionary<string, string>
            {
                { "Configuration", debugMode ? "Debug" : "Release" },
            };

            return new MsBuildProjectCollection(projectPaths, hostContext, logger, configOptions);
        }

        /// <summary>
        /// Get the set of all nuget package dependencies across the graph of projects.
        /// </summary>
        /// <returns>The package dependencies of the loaded projects.</returns>
        public IEnumerable<PackageDependency> GetAllDependencies()
        {
            var allDeps = new HashSet<PackageDependency>(PackageDependencyComparer.Default);

            foreach (var projectNode in graph.ProjectNodes)
            {
                VisitProjectForDependencies(projectNode.ProjectInstance, allDeps);
            }

            return allDeps;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            projectCollection.Dispose();
        }

        private void VisitProjectForDependencies(ProjectInstance project, HashSet<PackageDependency> dependencies)
        {
            var packages = project.GetItems("PackageReference");

            foreach (var package in packages)
            {
                var packageId = package.EvaluatedInclude;
                var packageVersion = package.GetMetadataValue("Version");
                var privateAssets = package.GetMetadataValue("PrivateAssets");
                var excludeAssets = package.GetMetadataValue("ExcludeAssets");

                if (privateAssets.Contains("all", StringComparison.InvariantCultureIgnoreCase) ||
                    privateAssets.Contains("runtime", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Don't include packages if they won't be copied to the output.
                    continue;
                }
                else if (excludeAssets.Contains("runtime", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Don't consume packages that don't bring over the runtime.
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

        /// <summary>
        /// Gets the set of projects in the collection as packages that can be installed.
        /// </summary>
        /// <returns>The set of project packages.</returns>
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

                yield return new LocalProjectPackage(proj.ProjectInstance.Directory, packageId, version, outputDirectory, outputDllName);
            }
        }

        /// <summary>
        /// Build the projects in the collection.
        /// </summary>
        /// <returns>An awaitable that on completion will contain true on success, and false on failure.</returns>
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
                    "-v",
                    "minimal",
                    "-c",
                    projectCollection.GlobalProperties["Configuration"]);

                var exitCode = await processRun.Run().ConfigureAwait(false);

                if (exitCode == 0)
                {
                    logger.LogDebug(processRun.Output);
                }
                else
                {
                    success = false;
                    logger.LogError(processRun.Output);
                }
            }

            return success;
        }
    }
}
