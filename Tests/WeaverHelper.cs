using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

using NUnit.Framework;

using TomsToolbox.Core;

namespace Tests
{
    internal class WeaverHelper : DefaultAssemblyResolver
    {
        [NotNull]
        private static readonly Dictionary<string, WeaverHelper> _cache = new Dictionary<string, WeaverHelper>();

        [NotNull]
        public Assembly Assembly { get; }
        [NotNull]
        public string NewAssemblyPath { get; }
        [NotNull]
        public string OriginalAssemblyPath { get; }
        [NotNull, ItemNotNull]
        public IList<string> Errors { get; } = new List<string>();
        [NotNull, ItemNotNull]
        public IList<string> Messages { get; } = new List<string>();

#if (!DEBUG)
        private const string Configuration = "Release";
#else
        private const string Configuration = "Debug";
#endif

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
            // ReSharper disable once AssignNullToNotNullAttribute
            // ReSharper disable once PossibleNullReferenceException
            var projectDir = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, $@"..\..\..\..\{assemblyName}"));

            var binaryDir = Path.Combine(projectDir, @"bin", Configuration, "Net45");
            OriginalAssemblyPath = Path.Combine(binaryDir, $@"{assemblyName}.dll");

            NewAssemblyPath = OriginalAssemblyPath.Replace(".dll", "2.dll");

            RegisterAssembly(AssemblyDefinition.ReadAssembly(Path.Combine(binaryDir, "TestLibrary.dll")));
            RegisterAssembly(AssemblyDefinition.ReadAssembly(Path.Combine(binaryDir, "AutoProperties.dll")));

            using (var moduleDefinition = ModuleDefinition.ReadModule(OriginalAssemblyPath, new ReaderParameters { ReadSymbols = true, AssemblyResolver = this }))
            {
                Debug.Assert(moduleDefinition != null, "moduleDefinition != null");

                var weavingTask = new ModuleWeaver
                {
                    ModuleDefinition = moduleDefinition,
                    // ReSharper disable once AssignNullToNotNullAttribute
                    AssemblyResolver = moduleDefinition.AssemblyResolver
                };

                weavingTask.LogErrorPoint += WeavingTask_LogErrorPoint;
                weavingTask.LogError += WeavingTask_LogError;
                weavingTask.LogInfo += WeavingTask_LogInfo;
                weavingTask.LogDebug += WeavingTask_LogDebug;

                weavingTask.Execute();

                var assemblyNameDefinition = moduleDefinition.Assembly?.Name;
                Debug.Assert(assemblyNameDefinition != null, "assemblyNameDefinition != null");

                // ReSharper disable once PossibleNullReferenceException
                assemblyNameDefinition.Version = new Version(0, 2, 0, assemblyNameDefinition.Version.Revision);
                moduleDefinition.Write(NewAssemblyPath, new WriterParameters { WriteSymbols = true });
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            Assembly = Assembly.LoadFile(NewAssemblyPath);
        }

        private void WeavingTask_LogInfo([NotNull] string message)
        {
            Messages.Add("I:" + message);
        }

        private void WeavingTask_LogDebug([NotNull] string message)
        {
            Messages.Add("D:" + message);
        }

        private void WeavingTask_LogError([NotNull] string message)
        {
            Messages.Add("E:" + message);
            Errors.Add(message);
        }

        private void WeavingTask_LogErrorPoint([NotNull] string message, [CanBeNull] SequencePoint sequencePoint)
        {
            if (sequencePoint != null)
            {
                message = message + $"\r\n\t({sequencePoint.Document.Url}@{sequencePoint.StartLine}:{sequencePoint.StartColumn}\r\n\t => {File.ReadAllLines(sequencePoint.Document.Url).Skip(sequencePoint.StartLine - 1).FirstOrDefault()}";
            }

            Messages.Add("E:" + message);
            Errors.Add(message);
        }
    }
}
