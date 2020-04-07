using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStep.Extensions
{
    public class ExtensionConfiguration
    {
        public string Package { get; set; }

        public string? Version { get; set; }

        public bool PreRelease { get; set; }

        public bool IgnoreInteractionFiles { get; set; }
    }
}
