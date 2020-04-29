using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoStep.Extensions.LocalExtensions.Build
{
    public class LocalProjectPackage
    {
        public LocalProjectPackage(string projectName, string projectVersion, string binaryDirectory, string entryPointDllName)
        {
            ProjectName = projectName;
            ProjectVersion = projectVersion;
            BinaryDirectory = binaryDirectory;
            EntryPointDllName = entryPointDllName;
        }

        public string ProjectName { get; }

        public string ProjectVersion { get; }

        public string? BinaryDirectory { get; }

        public string EntryPointDllName { get; }
    }
}
