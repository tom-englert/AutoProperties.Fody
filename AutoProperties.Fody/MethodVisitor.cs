#pragma warning disable CCRSI_ContractForNotNull // Element with [NotNull] attribute does not have a corresponding not-null contract.
#pragma warning disable CCRSI_CreateContractInvariantMethod // Missing Contract Invariant Method.

using System;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoProperties.Fody
{
    internal static class MethodVisitorExtensions
    {
        internal static void VisitAllMethods([NotNull] this ModuleDefinition moduleDefinition, [NotNull] ILogger logger)
        {
            new MethodVisitor(moduleDefinition, logger).VisitAllMethods();
        }
    }

    internal class MethodVisitor
    {
        [NotNull]
        private readonly ModuleDefinition _moduleDefinition;
        [NotNull]
        private readonly ILogger _logger;

        private readonly ISymbolReader _symbolReader;

        public MethodVisitor([NotNull] ModuleDefinition moduleDefinition, [NotNull] ILogger logger)
        {
            _logger = logger;
            _moduleDefinition = moduleDefinition;
            _symbolReader = moduleDefinition.SymbolReader;
        }

        internal void VisitAllMethods()
        {
            var allTypes = _moduleDefinition.GetTypes();

            // ReSharper disable once AssignNullToNotNullAttribute
            var allClasses = allTypes
                .Where(x => x != null && x.IsClass && (x.BaseType != null));

            foreach (var classDefinition in allClasses)
            {
                var shouldBypassAutoPropertySetters = classDefinition.ShouldBypassAutoPropertySettersInConstructors()
                                                      ?? _moduleDefinition.Assembly.ShouldBypassAutoPropertySettersInConstructors()
                                                      ?? false;

                var autoPropertyToBackingFieldMap = new AutoPropertyToBackingFieldMap(classDefinition);

                // ReSharper disable once AssignNullToNotNullAttribute
                // ReSharper disable once PossibleNullReferenceException
                var allMethods = classDefinition.Methods.Where(method => method.HasBody);

                foreach (var method in allMethods)
                {
                    if (method.IsConstructor && shouldBypassAutoPropertySetters)
                        BypassAutoPropertySetters(method, autoPropertyToBackingFieldMap);

                    ProcessExtensionMethodCalls(method, autoPropertyToBackingFieldMap);
                }
            }
        }

        private void BypassAutoPropertySetters([NotNull] MethodDefinition method, [NotNull] AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
        {
            // ReSharper disable once PossibleNullReferenceException
            var instructions = method.Body.Instructions;

            // ReSharper disable once PossibleNullReferenceException
            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                // ReSharper disable once AssignNullToNotNullAttribute
                if (!instruction.IsPropertySetterCall(out string propertyName))
                    continue;

                if (!autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                    continue;

                _logger.LogInfo($"Replace setter of property {propertyName} in method {method.FullName} with backing field assignment.");

                instructions[index] = Instruction.Create(OpCodes.Stfld, propertyInfo.BackingField);
            }
        }

        private void ProcessExtensionMethodCalls([NotNull] MethodDefinition method, [NotNull] AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
        {
            var processor = new ExtensionMethodProcessor(_logger, _symbolReader, method, autoPropertyToBackingFieldMap);

            // ReSharper disable PossibleNullReferenceException
            processor.ProcessExtensionMethodCalls("SetBackingField", pi => Instruction.Create(OpCodes.Stfld, pi.BackingField));
            processor.ProcessExtensionMethodCalls("SetProperty", pi => Instruction.Create(OpCodes.Call, pi.Property.SetMethod));
            // ReSharper restore PossibleNullReferenceException
        }

        private class ExtensionMethodProcessor
        {
            [NotNull]
            private readonly ILogger _logger;
            [NotNull]
            private readonly MethodDefinition _method;
            [NotNull]
            private readonly AutoPropertyToBackingFieldMap _autoPropertyToBackingFieldMap;

            [CanBeNull]
            private readonly MethodDebugInformation _debugInformation;

            public ExtensionMethodProcessor([NotNull] ILogger logger, [CanBeNull] ISymbolReader symbolReader, [NotNull] MethodDefinition method, [NotNull] AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
            {
                _logger = logger;
                _method = method;
                _autoPropertyToBackingFieldMap = autoPropertyToBackingFieldMap;

                _debugInformation = symbolReader?.Read(method);
            }

            public void ProcessExtensionMethodCalls([NotNull] string extensionMethodName, [NotNull] Func<AutoPropertyInfo, Instruction> createInstruction)
            {
                // ReSharper disable once PossibleNullReferenceException
                var instructions = _method.Body.Instructions;

                // ReSharper disable once PossibleNullReferenceException
                for (var index = 0; index < instructions.Count; index++)
                {
                    var instruction = instructions[index];

                    if (!instruction.IsExtensionMethodCall(extensionMethodName))
                        continue;

                    if ((_debugInformation == null) || !(_debugInformation.HasSequencePoints))
                    {
                        _logger.LogError($"No debug information for method {_method} found. Can't weave this method.");
                        return;
                    }

                    var sequencePoints = _debugInformation.SequencePoints;
                    // ReSharper disable once AssignNullToNotNullAttribute
                    // ReSharper disable once PossibleNullReferenceException
                    var sequencePoint = sequencePoints.LastOrDefault(sp => sp.Offset <= instruction.Offset);
                    // ReSharper disable once PossibleNullReferenceException
                    var nextSequencePoint = sequencePoints.SkipWhile(sp => sp.Offset <= instruction.Offset).FirstOrDefault();
                    if ((sequencePoint == null) || (nextSequencePoint == null))
                    {
                        _logger.LogError($"Incomplete debug information for method {_method}. Can't weave this method [1].");
                        return;
                    }

                    // ReSharper disable once PossibleNullReferenceException
                    var firstInstruction = instructions.SkipWhile(inst => inst.Offset < sequencePoint.Offset).FirstOrDefault();
                    // ReSharper disable once PossibleNullReferenceException
                    var lastInstruction = instructions.SkipWhile(inst => inst.Offset < nextSequencePoint.Offset).FirstOrDefault()?.Previous ?? instructions.Last();
                    if ((firstInstruction == null) || (lastInstruction == null))
                    {
                        _logger.LogError($"Incomplete debug information for method {_method}. Can't weave this method [2].");
                        return;
                    }
                    var secondInstruction = firstInstruction.Next;

                    while (lastInstruction?.OpCode == OpCodes.Nop)
                    {
                        lastInstruction = lastInstruction?.Previous;
                    }

                    var propertyName = string.Empty;

                    if ((firstInstruction.OpCode != OpCodes.Ldarg_0) 
                        || (secondInstruction?.IsPropertyGetterCall(out propertyName) != true)
                        // ReSharper disable once AssignNullToNotNullAttribute
                        || !_autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo)
                        || (lastInstruction != instruction))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        var message = $"Invalid usage of extension method '{extensionMethodName}()': '{extensionMethodName}()' is only valid on auto-properties of class {_method.DeclaringType.Name}";
                        _logger.LogError(message, sequencePoint);
                        return;
                    }

                    _logger.LogInfo($"Replace {extensionMethodName}() on property {propertyName} in method {_method}.");

                    instructions[index] = createInstruction(propertyInfo);
                    instructions.Remove(secondInstruction);
                    index -= 1;
                }
            }
        }
    }
}
