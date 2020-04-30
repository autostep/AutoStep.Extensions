using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace LocalExtensionDependency
{
    public class MyClass
    {
        public MyClass()
        {
            new LoggerConfiguration().CreateLogger();
        }
    }
}
