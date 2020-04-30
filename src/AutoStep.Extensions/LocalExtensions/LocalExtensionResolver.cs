using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.LocalExtensions;
using AutoStep.Extensions.LocalExtensions.Build;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AutoStep.Extensions
{
    /// <summary>
    /// Implements the resolver for finding and resolving (building) local MSBuild projects.
    /// </summary>
    internal class LocalExtensionResolver : IExtensionPackagesResolver
    {
        private static readonly IReadOnlyList<string> SupportedProjectExtensions = new[]
        {
            ".csproj",
            ".vbproj",
            ".fsproj",
        };

        private readonly IHostContext hostContext;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalExtensionResolver"/> class.
        /// </summary>
        /// <param name="hostContext">The host context.</param>
        /// <param name="logger">A logger.</param>
        public LocalExtensionResolver(IHostContext hostContext, ILogger logger)
        {
            this.hostContext = hostContext;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async ValueTask<IInstallablePackageSet> ResolvePackagesAsync(ExtensionResolveContext resolveContext, CancellationToken cancelToken)
        {
            if (!resolveContext.FolderExtensions.Any())
            {
                // No folder extensions, short-circuit this.
                return EmptyValidPackageSet.Instance;
            }

            var projectFilePaths = new List<string>();
            var isValid = true;

            // Locate the project paths for each configured folder.
            foreach (var folder in resolveContext.FolderExtensions)
            {
                string? projectPath = ResolveProjectPath(folder);

                if (projectPath is null)
                {
                    isValid = false;
                }
                else
                {
                    projectFilePaths.Add(projectPath);
                }
            }

            if (isValid)
            {
                try
                {
                    logger.LogInformation(LocalExtensionResolverMessages.BuildingProjects);

                    MsBuildLibraryLoader.EnsureLoaded();

                    using var msBuildContext = MsBuildProjectCollection.Create(projectFilePaths, logger, hostContext);

                    var allDeps = msBuildContext.GetAllDependencies();

                    // Load context.
                    var projectBuildSuccess = await msBuildContext.Build();

                    if (projectBuildSuccess)
                    {
                        resolveContext.AdditionalPackagesRequired.AddRange(allDeps);

                        logger.LogInformation(LocalExtensionResolverMessages.ProjectsBuiltOk);

                        var output = msBuildContext.GetProjectsAsInstallablePackages().ToList();

                        return new LocalInstallableSet(output, hostContext);
                    }
                    else
                    {
                        throw new ExtensionLoadException(LocalExtensionResolverMessages.BuildFailure);
                    }
                }
                catch (ExtensionLoadException ex)
                {
                    return new InvalidPackageSet(ex);
                }
                catch (InvalidOperationException)
                {
                    // Could not find MSBuild.
                    return new InvalidPackageSet(new ExtensionLoadException(LocalExtensionResolverMessages.NoSDK));
                }
            }
            else
            {
                return new InvalidPackageSet();
            }
        }

        private string? ResolveProjectPath(FolderExtensionConfiguration folder)
        {
            if (folder.Folder is null)
            {
                logger.LogError(LocalExtensionResolverMessages.NoFolderProvided);

                return null;
            }

            var directory = folder.Folder;

            if (!Path.IsPathFullyQualified(directory))
            {
                directory = Path.GetFullPath(directory, hostContext.RootDirectory);
            }

            var directoryInfo = new DirectoryInfo(directory);

            if (directoryInfo.Exists)
            {
                // Look for the first project file in that folder.
                var projectFile = directoryInfo.EnumerateFiles().FirstOrDefault(x => SupportedProjectExtensions.Contains(x.Extension));

                if (projectFile is object)
                {
                    logger.LogDebug(LocalExtensionResolverMessages.IncludingProjectFile, projectFile.FullName);

                    return projectFile.FullName;
                }
                else
                {
                    logger.LogError(LocalExtensionResolverMessages.FolderDoesNotHaveAProjectFile, directoryInfo.FullName);
                }
            }
            else
            {
                logger.LogError(LocalExtensionResolverMessages.ExtensionFolderDoesNotExist, directory);
            }

            return null;
        }
    }
}
