using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using AutoStep.Extensions.Build;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AutoStep.Extensions
{
    internal class LocalExtensionLoader
    {
        private readonly string localExtensionsDirectory;
        private IHostContext hostContext;
        private ILogger logger;

        public LocalExtensionLoader(string localExtensionsDirectory, IHostContext hostContext, ILogger logger)
        {
            this.localExtensionsDirectory = localExtensionsDirectory;
            this.hostContext = hostContext;
            this.logger = logger;
        }

        public async ValueTask<IEnumerable<PackageDependency>> GetRequiredDependenciesAsync(IEnumerable<FolderExtensionConfiguration> folderExtensions)
        {
            var allDeps = new HashSet<PackageDependency>(PackageDependencyComparer.Default);

            var projectFilePaths = new List<string>();

            foreach (var folder in folderExtensions)
            {
                string projectPath = Path.Combine(folder.Folder, folder.Name + ".csproj");

                if (!Path.IsPathFullyQualified(folder.Folder))
                {
                    projectPath = Path.GetFullPath(projectPath, localExtensionsDirectory);
                }

                if (File.Exists(projectPath))
                {
                    // Does the project file exist?
                    projectFilePaths.Add(projectPath);
                }
            }

            if (projectFilePaths.Any())
            {
                try
                {
                    logger.LogInformation("Building Local Projects");

                    var msBuildPath = MsBuildLibraryLoader.GetMsBuildPath();

                    using var msBuildContext = MsBuildLoadContext.Create(projectFilePaths, msBuildPath, logger, hostContext);

                    msBuildContext.PopulateDependencies(allDeps);

                    // Load context.
                    var outputBinaries = msBuildContext.GetOutputBinaries();

                    var projectBuildSuccess = await msBuildContext.Build();

                    if (projectBuildSuccess)
                    {
                        throw new ExtensionLoadException("One of more of the configured local code projects could not be built, so we cannot continue.");
                    }
                }
                catch (ExtensionLoadException)
                {
                    throw;
                }
                catch (InvalidOperationException)
                {
                    // Could not find MSBuild.
                    throw new ExtensionLoadException("Could not find an installed dotnet SDK on this machine. Using folder extensions with code requires the dotnet SDK.");
                }
            }

            return allDeps;
        }

        private System.Reflection.Assembly? Default_Resolving(AssemblyLoadContext arg1, System.Reflection.AssemblyName arg2)
        {
            throw new NotImplementedException();
        }
    }
}
