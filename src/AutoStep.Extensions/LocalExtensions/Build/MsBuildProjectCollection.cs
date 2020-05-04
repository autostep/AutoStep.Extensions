using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Definition;
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
        private const string ConfigurationProp = "Configuration";
        private const string ConfigurationValueDebug = "Debug";
        private const string ConfigurationValueRelease = "Release";

        private const string ItemTypePackageReference = "PackageReference";
        private const string VersionMeta = "Version";
        private const string PrivateAssetsMeta = "PrivateAssets";
        private const string ExcludeAssetsMeta = "ExcludeAssets";

        private const string AssetsOptionAll = "all";
        private const string AssetsOptionRuntime = "runtime";

        private const string PackageIdProp = "PackageId";
        private const string MsBuildProjectNameProp = "MSBuildProjectName";
        private const string VersionProp = "Version";

        private const string TargetDirProp = "TargetDir";
        private const string AssemblyNameProp = "AssemblyName";

        private const string ItemTypeCompile = "Compile";
        private const string ItemTypeEmbeddedResource = "EmbeddedResource";
        private const string ItemTypeContent = "Content";

        private const string CopyToOutputDirectoryMeta = "CopyToOutputDirectory";
        private const string PreserveNewestMetaValue = "PreserveNewest";
        private const string AlwaysMetaValue = "Always";

        private readonly IHostContext hostContext;
        private readonly ILogger logger;
        private readonly ProjectCollection projectCollection;
        private readonly ProjectGraph graph;

        private MsBuildProjectCollection(IEnumerable<string> projects, IHostContext hostContext, ILogger logger, IDictionary<string, string> configOptions, CancellationToken cancelToken)
        {
            projectCollection = new ProjectCollection(configOptions);

            // Construct the graph of projects.
            graph = new ProjectGraph(
                projects.Select(x => new ProjectGraphEntryPoint(x, configOptions)),
                projectCollection,
                CreateProjectInstance,
                cancelToken);

            this.hostContext = hostContext;
            this.logger = logger;
        }

        private ProjectInstance CreateProjectInstance(string projectPath, Dictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            var project = projectCollection.LoadProject(projectPath);

            return project.CreateProjectInstance(ProjectInstanceSettings.ImmutableWithFastItemLookup);
        }

        /// <summary>
        /// Create a new project collection, given a set of project paths.
        /// </summary>
        /// <param name="projectPaths">The set of absolute project paths.</param>
        /// <param name="logger">A logger for the collection.</param>
        /// <param name="hostContext">The host context.</param>
        /// <param name="cancelToken">A cancellation token for the project graph build.</param>
        /// <param name="debugMode">Whether to use the Debug project configuration.</param>
        /// <returns>A new project collection.</returns>
        public static MsBuildProjectCollection Create(IEnumerable<string> projectPaths, ILogger logger, IHostContext hostContext, CancellationToken cancelToken, bool debugMode = false)
        {
            // Use MSBuild.
            var configOptions = new Dictionary<string, string>
            {
                { ConfigurationProp, debugMode ? ConfigurationValueDebug : ConfigurationValueRelease },
            };

            return new MsBuildProjectCollection(projectPaths, hostContext, logger, configOptions, cancelToken);
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

        private void VisitProjectForDependencies(ProjectInstance project, HashSet<PackageDependency> dependencies)
        {
            var packages = project.GetItems(ItemTypePackageReference);

            foreach (var package in packages)
            {
                var packageId = package.EvaluatedInclude;
                var packageVersion = package.GetMetadataValue(VersionMeta);
                var privateAssets = package.GetMetadataValue(PrivateAssetsMeta);
                var excludeAssets = package.GetMetadataValue(ExcludeAssetsMeta);

                if (privateAssets.Contains(AssetsOptionAll, StringComparison.InvariantCultureIgnoreCase) ||
                    privateAssets.Contains(AssetsOptionRuntime, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Don't include packages if they won't be copied to the output.
                    continue;
                }
                else if (excludeAssets.Contains(AssetsOptionRuntime, StringComparison.InvariantCultureIgnoreCase))
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
        /// Gets the metadata for a given project path.
        /// </summary>
        /// <param name="projectPath">The project file path.</param>
        /// <param name="includeSourceFiles">Whether to collect project source file information.</param>
        /// <returns>A metadata block.</returns>
        public ProjectMetadata GetProjectMetadata(string projectPath, bool includeSourceFiles)
        {
            var project = graph.ProjectNodes.FirstOrDefault(x => x.ProjectInstance.FullPath == projectPath)?.ProjectInstance;

            if (project is null)
            {
                throw new ArgumentException(BuildMessages.ProjectNotInCollection, nameof(projectPath));
            }

            var packageId = project.GetPropertyValue(PackageIdProp);

            if (string.IsNullOrEmpty(packageId))
            {
                packageId = project.GetPropertyValue(MsBuildProjectNameProp);
            }

            var version = project.GetPropertyValue(VersionProp);

            if (string.IsNullOrEmpty(version))
            {
                // Default it.
                version = "1.0.0";
            }

            var outputDirectory = project.GetPropertyValue(TargetDirProp);
            var outputDllName = project.GetPropertyValue(AssemblyNameProp) + ".dll";

            return new ProjectMetadata(
                project.FullPath,
                project.Directory,
                packageId,
                version,
                outputDirectory,
                outputDllName,
                includeSourceFiles ? GetSourceFiles(project) : Array.Empty<string>());
        }

        private IReadOnlyList<string> GetSourceFiles(ProjectInstance project)
        {
            var files = new List<string>();

            // Go through the project and collect all Compile, EmbeddedResource, and Content files (where the Content is copied to the output).
            foreach (var compileItem in project.GetItems(ItemTypeCompile))
            {
                // Store the full path.
                files.Add(Path.GetFullPath(compileItem.EvaluatedInclude, project.Directory));
            }

            foreach (var embeddedResource in project.GetItems(ItemTypeEmbeddedResource))
            {
                // Store the full path.
                files.Add(Path.GetFullPath(embeddedResource.EvaluatedInclude, project.Directory));
            }

            foreach (var content in project.GetItems(ItemTypeContent))
            {
                var copyToOutput = content.GetMetadataValue(CopyToOutputDirectoryMeta);

                if (copyToOutput.Equals(PreserveNewestMetaValue, StringComparison.InvariantCultureIgnoreCase) ||
                    copyToOutput.Equals(AlwaysMetaValue, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Store the full path.
                    files.Add(Path.GetFullPath(content.EvaluatedInclude, project.Directory));
                }
            }

            return files;
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

        /// <inheritdoc/>
        public void Dispose()
        {
            projectCollection.Dispose();
        }
    }
}
