using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions
{
    internal abstract class BaseExtensionLoader
    {
        private readonly string runtimeTarget;        
        private readonly FrameworkReducer frameworkReducer;

        protected BaseExtensionLoader(string frameworkName)
        {
            runtimeTarget = frameworkName;

            TargetFramework = NuGetFramework.ParseFrameworkName(runtimeTarget, DefaultFrameworkNameProvider.Instance);
            frameworkReducer = new FrameworkReducer();
        }

        protected NuGetFramework TargetFramework { get; }

        protected IEnumerable<string> GetFrameworkFiles(IEnumerable<FrameworkSpecificGroup> frameworkGroup)
        {
            var nearest = frameworkReducer.GetNearest(TargetFramework, frameworkGroup.Select(x => x.TargetFramework));

            var selectedItems = frameworkGroup.Where(x => x.TargetFramework.Equals(nearest))
                                              .SelectMany(x => x.Items);

            return selectedItems;
        }

        protected IEnumerable<string> GetRuntimeLibraryLibPaths(RuntimeLibrary library)
        {
            var targetRuntimeGroup = library.RuntimeAssemblyGroups.FirstOrDefault(t => string.IsNullOrEmpty(t.Runtime) || t.Runtime == runtimeTarget);

            if (targetRuntimeGroup == null)
            {
                return null;
            }

            return targetRuntimeGroup.AssetPaths;
        }
    }
}
