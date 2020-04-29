using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using AutoStep.Extensions.Build;
using AutoStep.Extensions.LocalExtensions;
using AutoStep.Extensions.LocalExtensions.Build;
using AutoStep.Projects;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Core;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AutoStep.Extensions
{
    internal class LocalExtensionResolver : IExtensionPackagesResolver
    {
        private IHostContext hostContext;
        private ILogger logger;

        private static readonly IReadOnlyList<string> SupportedProjectExtensions = new[]
        {
            ".csproj", ".vbproj", ".fsproj"
        };

        public LocalExtensionResolver(IHostContext hostContext, ILogger logger)
        {
            this.hostContext = hostContext;
            this.logger = logger;
        }

        public async ValueTask<IInstallablePackageSet> ResolvePackagesAsync(ExtensionResolveContext resolveContext, CancellationToken cancelToken)
        {
            if (!resolveContext.FolderExtensions.Any())
            {
                // No folder extensions, short-circuit this.
                return EmptyValidPackageSet.Instance;
            }

            var projectFilePaths = new List<string>();
            var isValid = true;

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
                    logger.LogInformation("Building Local Projects.");

                    MsBuildLibraryLoader.EnsureLoaded();

                    using var msBuildContext = MsBuildLoadContext.Create(projectFilePaths, logger, hostContext);

                    var allDeps = msBuildContext.GetAllDependencies();

                    // Load context.
                    var projectBuildSuccess = await msBuildContext.Build();

                    if (projectBuildSuccess)
                    {
                        resolveContext.AdditionalPackagesRequired.AddRange(allDeps);

                        logger.LogInformation("Local Projects Built Successfully.");

                        var output = msBuildContext.GetProjectsAsInstallablePackages().ToList();

                        return new LocalInstallableSet(output, hostContext);
                    }
                    else
                    {
                        throw new ExtensionLoadException("One of more of the configured local projects could not be built, so we cannot continue.");
                    }
                }
                catch (ExtensionLoadException ex)
                {
                    return new InvalidPackageSet(ex);
                }
                catch (InvalidOperationException)
                {
                    // Could not find MSBuild.
                    return new InvalidPackageSet(new ExtensionLoadException("Could not find an installed dotnet SDK on this machine. Using folder extensions with code requires the dotnet SDK."));
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
                logger.LogError("No folder provided for configured local extension.");

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
                    logger.LogDebug("Including .NET Project File '{0}' as a local extension.", projectFile.FullName);

                    return projectFile.FullName;
                }
                else
                {
                    logger.LogError("Configured extension folder '{0}' does not contain a .NET project file.");
                }
            }
            else
            {
                logger.LogError("Configured extension folder '{0}' does not exist.", directory);
            }

            return null;
        }
    }
}
