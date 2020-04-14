using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Fody;

using Mono.Cecil;

using TomsToolbox.Core;

namespace Tests
{
    using AutoProperties.Fody;

    internal class WeaverHelper : DefaultAssemblyResolver
    {
        private static readonly Dictionary<string, WeaverHelper> _cache = new Dictionary<string, WeaverHelper>();

        private readonly TestResult _testResult;

        public Assembly Assembly => _testResult.Assembly;

        public IEnumerable<string> Errors => _testResult.Errors.Select(e => e.FormatError());

        public IEnumerable<string> Messages => _testResult.Messages.Select(m => m.FormatMessage());

        public static WeaverHelper Create(string assemblyName = "AssemblyToProcess")
        {
            lock (typeof(WeaverHelper))
            {
                return _cache.ForceValue(assemblyName, _ => new WeaverHelper(assemblyName));
            }
        }

        private WeaverHelper(string assemblyName)
        {
            _testResult = new ModuleWeaver().ExecuteTestRun(assemblyName + ".dll", true, null, null, null, new[] { "0x80131869" });
        }
    }
}
