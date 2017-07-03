using System;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace AutoProperties.Fody
{
    internal static class MethodVisitor
    {
        internal static void VisitAllMethods(this ModuleDefinition moduleDefinition, ILogger logger)
        {
            var allTypes = moduleDefinition.GetTypes();

            var allClasses = allTypes
                .Where(x => x.IsClass && (x.BaseType != null));

            foreach (var classDefinition in allClasses)
            {
                var shouldBypassAutoPropertySetters = classDefinition.ShouldBypassAutoPropertySettersInConstructors() 
                    ?? moduleDefinition.ShouldBypassAutoPropertySettersInConstructors() 
                    ?? false;

                var autoPropertyToBackingFieldMap = new AutoPropertyToBackingFieldMap(classDefinition);

                var allMethods = classDefinition.Methods.Where(method => method.HasBody);

                foreach (var method in allMethods)
                {
                    var instructions = method.Body.Instructions;

                    if (method.IsConstructor && shouldBypassAutoPropertySetters)
                        instructions.BypassAutoPropertySetters(autoPropertyToBackingFieldMap);

                    instructions.ProcessSetBackingFieldCalls(autoPropertyToBackingFieldMap, logger);
                    instructions.ProcessSetPropertyCalls(autoPropertyToBackingFieldMap, logger);
                }
            }
        }

        private static void BypassAutoPropertySetters(this Collection<Instruction> instructions, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap)
        {
            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsPropertySetterCall(out string propertyName))
                    continue;

                if (!autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                    continue;

                instructions[index] = Instruction.Create(OpCodes.Stfld, propertyInfo.BackingField);
            }
        }

        private static void ProcessSetBackingFieldCalls(this Collection<Instruction> instructions, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap, ILogger logger)
        {
            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsSetBackingFieldCall())
                    continue;

                var propertyName = String.Empty;

                if ((instruction.Previous?.Previous?.IsPropertyGetterCall(out propertyName) != true) 
                    || !autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                {
                    logger.LogWarning("Invalid usage of extension method 'SetBackingField'. This is only valid on member auto-properties.");
                    return;
                }

                instructions[index] = Instruction.Create(OpCodes.Stfld, propertyInfo.BackingField);
                instructions.RemoveAt(index - 2);
                index -= 1;
            }
        }

        private static void ProcessSetPropertyCalls(this Collection<Instruction> instructions, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap, ILogger logger)
        {
            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsSetPropertyCall())
                    continue;

                var propertyName = String.Empty;

                if ((instruction.Previous?.Previous?.IsPropertyGetterCall(out propertyName) != true)
                    || !autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                {
                    logger.LogWarning("Invalid usage of extension method 'SetProperty'. This is only valid on member auto-properties.");
                    return;
                }

                instructions[index] = Instruction.Create(OpCodes.Call, propertyInfo.Property.SetMethod);
                instructions.RemoveAt(index - 2);
                index -= 1;
            }
        }
    }
}
