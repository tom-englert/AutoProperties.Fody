using System;
using System.Diagnostics;
using System.Linq;
using FodyTools;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoProperties.Fody
{
    internal class BackingFieldAccessWeaver
    {
        [NotNull]
        private readonly ModuleDefinition _moduleDefinition;
        [NotNull]
        private readonly ILogger _logger;
        [CanBeNull]
        private readonly ISymbolReader _symbolReader;

        public BackingFieldAccessWeaver([NotNull] ModuleDefinition moduleDefinition, [NotNull] ILogger logger)
        {
            _logger = logger;
            _moduleDefinition = moduleDefinition;
            _symbolReader = moduleDefinition.SymbolReader;
        }

        internal void Execute()
        {
            var allTypes = _moduleDefinition.GetTypes();

            // ReSharper disable once AssignNullToNotNullAttribute
            var allClasses = allTypes
                .Where(x => x != null && x.IsClass && (x.BaseType != null));

            try
            {
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
                        {
                            BypassAutoPropertySetters(method, autoPropertyToBackingFieldMap);
                        }

                        ProcessExtensionMethodCalls(method, autoPropertyToBackingFieldMap);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unhandled exception. Weaving aborted. The most probable reason is that the module has no or incompatible debug information (.pdb)");
                _logger.LogDebug(ex.ToString());
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
            [NotNull, ItemNotNull]
            private readonly InstructionSequences _instructionSequences;

            public ExtensionMethodProcessor([NotNull] ILogger logger, [CanBeNull] ISymbolReader symbolReader, [NotNull] MethodDefinition method, [NotNull] AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
            {
                _logger = logger;
                _method = method;
                _autoPropertyToBackingFieldMap = autoPropertyToBackingFieldMap;

                Debug.Assert(method.Body?.Instructions != null, "method.Body.Instructions != null");

                _instructionSequences = new InstructionSequences(method.Body.Instructions, method.ReadSequencePoints(symbolReader));
            }

            public void ProcessExtensionMethodCalls([NotNull] string extensionMethodName, [NotNull] Func<AutoPropertyInfo, Instruction> createInstruction)
            {
                foreach (var sequence in _instructionSequences)
                {
                    Debug.Assert(sequence != null, "sequence != null");

                    if (!ProcessSequence(sequence, extensionMethodName, createInstruction))
                        return;
                }
            }

            private bool ProcessSequence([NotNull, ItemNotNull] InstructionSequence sequence, [NotNull] string extensionMethodName, [NotNull] Func<AutoPropertyInfo, Instruction> createInstruction)
            {
                for (var index = 0; index < sequence.Count; index++)
                {
                    var instruction = sequence[index];

                    if (!instruction.IsExtensionMethodCall(extensionMethodName))
                        continue;

                    if (sequence.Count < 4
                        || sequence[0].OpCode != OpCodes.Ldarg_0
                        || !sequence[1].IsPropertyGetterCall(out string propertyName)
                        || sequence.Skip(index + 1).Any(inst => inst?.OpCode != OpCodes.Nop)
                        // ReSharper disable once AssignNullToNotNullAttribute
                        || !_autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                    {
                        var message = $"Invalid usage of extension method '{extensionMethodName}()': '{extensionMethodName}()' is only valid on auto-properties of class {_method.DeclaringType?.Name}";
                        _logger.LogError(message, sequence.Point);
                        return false;
                    }

                    _logger.LogInfo($"Replace {extensionMethodName}() on property {propertyName} in method {_method}.");

                    // ReSharper disable once AssignNullToNotNullAttribute
                    sequence[index] = createInstruction(propertyInfo);
                    sequence.RemoveAt(1);
                    return true;
                }

                return true;
            }
        }
    }
}
