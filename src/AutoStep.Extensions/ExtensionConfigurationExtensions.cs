using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace AutoStep.Extensions
{
    public static class ExtensionConfigurationExtensions
    {
        public static ExtensionConfiguration[] GetExtensionConfiguration(this IConfiguration config)
        {
            var all = config.GetSection("extensions").Get<ExtensionConfiguration[]>();

            if (all.Any(p => string.IsNullOrWhiteSpace(p.Package)))
            {
                throw new ProjectConfigurationException("Extensions must have a specified Package Id.");
            }

            return all;
        }
    }
}
