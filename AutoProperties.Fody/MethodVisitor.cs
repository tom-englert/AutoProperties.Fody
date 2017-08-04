using System;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoProperties.Fody
{
    internal static class MethodVisitorExtensions
    {
        internal static void VisitAllMethods(this ModuleDefinition moduleDefinition, ILogger logger)
        {
            new MethodVisitor(moduleDefinition, logger).VisitAllMethods();
        }
    }

    internal class MethodVisitor
    {
        private readonly ModuleDefinition _moduleDefinition;
        private readonly ILogger _logger;
        private readonly ISymbolReader _symbolReader;

        public MethodVisitor(ModuleDefinition moduleDefinition, ILogger logger)
        {
            _logger = logger;
            _moduleDefinition = moduleDefinition;
            _symbolReader = moduleDefinition.SymbolReader;
        }

        internal void VisitAllMethods()
        {
            var allTypes = _moduleDefinition.GetTypes();

            var allClasses = allTypes
                .Where(x => x.IsClass && (x.BaseType != null));

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
                        BypassAutoPropertySetters(method, autoPropertyToBackingFieldMap);

                    ProcessSetBackingFieldCalls(method, autoPropertyToBackingFieldMap);
                    ProcessSetPropertyCalls(method, autoPropertyToBackingFieldMap);
                }
            }
        }

        private void BypassAutoPropertySetters(MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
        {
            var instructions = method.Body.Instructions;

            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsPropertySetterCall(out string propertyName))
                    continue;

                if (!autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                    continue;

                _logger.LogInfo($"Replace setter of property {propertyName} in method {method.FullName} with backing field assignment.");

                instructions[index] = Instruction.Create(OpCodes.Stfld, propertyInfo.BackingField);
            }
        }

        private void ProcessSetBackingFieldCalls(MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
        {
            ProcessExtensionMethodCalls(method, autoPropertyToBackingFieldMap, "SetBackingField", pi => Instruction.Create(OpCodes.Stfld, pi.BackingField));
        }

        private void ProcessSetPropertyCalls(MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
        {
            ProcessExtensionMethodCalls(method, autoPropertyToBackingFieldMap, "SetProperty", pi => Instruction.Create(OpCodes.Call, pi.Property.SetMethod));
        }

        private void ProcessExtensionMethodCalls(MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap, string methodName, Func<AutoPropertyInfo, Instruction> createInstruction)
        {
            var instructions = method.Body.Instructions;

            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsExtensionMethodCall(methodName))
                    continue;

                var propertyName = string.Empty;

                if ((instruction.Previous?.Previous?.IsPropertyGetterCall(out propertyName) != true)
                    || !autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                {
                    var message = $"Invalid usage of extension method '{methodName}()': '{methodName}()' is only valid on auto-properties of class {method.DeclaringType.Name} and with simple parameters like constants, arguments or variables.";
                    _logger.LogError(message, _symbolReader?.Read(method)?.GetSequencePoint(instruction));
                    return;
                }

                _logger.LogInfo($"Replace {methodName}() on property {propertyName} in method {method.FullName}.");

                instructions[index] = createInstruction(propertyInfo);
                instructions.RemoveAt(index - 2);
                index -= 1;
            }
        }
    }
}
