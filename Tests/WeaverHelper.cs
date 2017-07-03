using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Mono.Cecil;

using NUnit.Framework;

using TomsToolbox.Core;

namespace Tests
{
    internal class WeaverHelper
    {
        private static readonly Dictionary<string, WeaverHelper> _cache = new Dictionary<string, WeaverHelper>();

        public Assembly Assembly { get; }
        public string NewAssemblyPath { get; }
        public string OriginalAssemblyPath { get; }

#if (!DEBUG)
        private const string Configuration = "Release";
#else
        private const string Configuration = "Debug";
#endif

        public static WeaverHelper Create(string assemblyName = "AssemblyToProcess")
        {
            lock (typeof(WeaverHelper))
            {
                return _cache.ForceValue(assemblyName, _ => new WeaverHelper(assemblyName));
            }
        }

        private WeaverHelper(string assemblyName)
        {
            var projectDir = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, $@"..\..\..\{assemblyName}"));
            var binaryDir = Path.Combine(projectDir, $@"bin\{Configuration}");
            OriginalAssemblyPath = Path.Combine(binaryDir, $@"{assemblyName}.dll");

            NewAssemblyPath = OriginalAssemblyPath.Replace(".dll", "2.dll");

            File.Copy(OriginalAssemblyPath, NewAssemblyPath, true);

            using (var moduleDefinition = ModuleDefinition.ReadModule(OriginalAssemblyPath))
            {
                var weavingTask = new ModuleWeaver
                {
                    ModuleDefinition = moduleDefinition
                };

                weavingTask.Execute();
                moduleDefinition.Write(NewAssemblyPath);
            }

            Assembly = Assembly.LoadFile(NewAssemblyPath);
        }
    }
}
