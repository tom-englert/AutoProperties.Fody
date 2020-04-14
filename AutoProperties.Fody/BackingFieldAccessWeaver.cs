namespace AutoProperties.Fody
{
    using System;
    using System.Linq;

    using FodyTools;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    internal class BackingFieldAccessWeaver
    {
        private readonly ModuleDefinition _moduleDefinition;
        private readonly ILogger _logger;
        private readonly ISymbolReader? _symbolReader;

        public BackingFieldAccessWeaver(ModuleDefinition moduleDefinition, ILogger logger)
        {
            _logger = logger;
            _moduleDefinition = moduleDefinition;
            _symbolReader = moduleDefinition.SymbolReader;
        }

        internal void Execute()
        {
            var allTypes = _moduleDefinition.GetTypes();

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

        private void BypassAutoPropertySetters(MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
        {
            var instructions = method.Body.Instructions;

            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsPropertySetterCall(out string? propertyName) || propertyName == null)
                    continue;

                if (!autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                    continue;

                _logger.LogInfo($"Replace setter of property {propertyName} in method {method.FullName} with backing field assignment.");

                instructions[index] = Instruction.Create(OpCodes.Stfld, propertyInfo.BackingField);
            }
        }

        private void ProcessExtensionMethodCalls(MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
        {
            var processor = new ExtensionMethodProcessor(_logger, _symbolReader, method, autoPropertyToBackingFieldMap);

            processor.ProcessExtensionMethodCalls("SetBackingField", pi => Instruction.Create(OpCodes.Stfld, pi.BackingField));
            processor.ProcessExtensionMethodCalls("SetProperty", pi => Instruction.Create(OpCodes.Call, pi.Property.SetMethod));
        }

        private class ExtensionMethodProcessor
        {
            private readonly ILogger _logger;
            private readonly MethodDefinition _method;
            private readonly AutoPropertyToBackingFieldMap _autoPropertyToBackingFieldMap;
            private readonly InstructionSequences _instructionSequences;

            public ExtensionMethodProcessor(ILogger logger, ISymbolReader? symbolReader, MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
            {
                _logger = logger;
                _method = method;
                _autoPropertyToBackingFieldMap = autoPropertyToBackingFieldMap;

                _instructionSequences = new InstructionSequences(method.Body.Instructions, method.ReadSequencePoints(symbolReader));
            }

            public void ProcessExtensionMethodCalls(string extensionMethodName, Func<AutoPropertyInfo, Instruction> createInstruction)
            {
                foreach (var sequence in _instructionSequences)
                {
                    if (!ProcessSequence(sequence, extensionMethodName, createInstruction))
                        return;
                }
            }

            private bool ProcessSequence(InstructionSequence sequence, string extensionMethodName, Func<AutoPropertyInfo, Instruction> createInstruction)
            {
                for (var index = 0; index < sequence.Count; index++)
                {
                    var instruction = sequence[index];

                    if (!instruction.IsExtensionMethodCall(extensionMethodName))
                        continue;

                    if (sequence.Count < 4
                        || sequence[0].OpCode != OpCodes.Ldarg_0
                        || !sequence[1].IsPropertyGetterCall(out string? propertyName)
                        || sequence.Skip(index + 1).Any(inst => inst?.OpCode != OpCodes.Nop)
                        || !_autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                    {
                        var message = $"Invalid usage of extension method '{extensionMethodName}()': '{extensionMethodName}()' is only valid on auto-properties of class {_method.DeclaringType?.Name}";
                        _logger.LogError(message, sequence.Point);
                        return false;
                    }

                    _logger.LogInfo($"Replace {extensionMethodName}() on property {propertyName} in method {_method}.");

                    sequence[index] = createInstruction(propertyInfo!);
                    sequence.RemoveAt(1);
                    return true;
                }

                return true;
            }
        }
    }
}
