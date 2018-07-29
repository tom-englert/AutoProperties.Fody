using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Fody;

using JetBrains.Annotations;

using Mono.Cecil;

using TomsToolbox.Core;

namespace Tests
{
    internal class WeaverHelper : DefaultAssemblyResolver
    {
        [NotNull]
        private static readonly Dictionary<string, WeaverHelper> _cache = new Dictionary<string, WeaverHelper>();

        [NotNull]
        private readonly TestResult _testResult;

        [NotNull]
        public Assembly Assembly => _testResult.Assembly;

        [NotNull, ItemNotNull]
        public IEnumerable<string> Errors => _testResult.Errors.Select(e => e.FormatError());

        [NotNull, ItemNotNull]
        public IEnumerable<string> Messages => _testResult.Messages.Select(m => m.FormatMessage());

        [NotNull]
        public static WeaverHelper Create([NotNull] string assemblyName = "AssemblyToProcess")
        {
            lock (typeof(WeaverHelper))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                return _cache.ForceValue(assemblyName, _ => new WeaverHelper(assemblyName));
            }
        }

        private WeaverHelper([NotNull] string assemblyName)
        {
            _testResult = new ModuleWeaver().ExecuteTestRun(assemblyName + ".dll", true, null, null, null, new[] { "0x80131869" });
        }
    }
}
