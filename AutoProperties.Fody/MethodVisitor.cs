using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

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
                    ?? moduleDefinition.Assembly.ShouldBypassAutoPropertySettersInConstructors() 
                    ?? false;

                var autoPropertyToBackingFieldMap = new AutoPropertyToBackingFieldMap(classDefinition);

                var allMethods = classDefinition.Methods.Where(method => method.HasBody);

                foreach (var method in allMethods)
                {
                    if (method.IsConstructor && shouldBypassAutoPropertySetters)
                        method.BypassAutoPropertySetters(autoPropertyToBackingFieldMap, logger);

                    method.ProcessSetBackingFieldCalls(autoPropertyToBackingFieldMap, logger);
                    method.ProcessSetPropertyCalls(autoPropertyToBackingFieldMap, logger);
                }
            }
        }

        private static void BypassAutoPropertySetters(this MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap, ILogger logger)
        {
            var instructions = method.Body.Instructions;

            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsPropertySetterCall(out string propertyName))
                    continue;

                if (!autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                    continue;

                logger.LogInfo($"Replace setter of property {propertyName} in method {method.FullName} with backing field assignment.");

                instructions[index] = Instruction.Create(OpCodes.Stfld, propertyInfo.BackingField);
            }
        }

        private static void ProcessSetBackingFieldCalls(this MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap, ILogger logger)
        {
            var instructions = method.Body.Instructions;
            var numberOfCalls = 0;

            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsSetBackingFieldCall())
                    continue;

                numberOfCalls += 1;

                var propertyName = string.Empty;

                if ((instruction.Previous?.Previous?.IsPropertyGetterCall(out propertyName) != true) 
                    || !autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                {
                    logger.LogError($"Invalid usage of extension method 'SetBackingField()': {numberOfCalls}. call in method {method.FullName}. 'SetBackingField()' is only valid on auto-properties of class {method.DeclaringType.Name}.");
                    return;
                }

                logger.LogInfo($"Replace SetBackingField() on property {propertyName} in method {method.FullName} with backing field assignment.");

                instructions[index] = Instruction.Create(OpCodes.Stfld, propertyInfo.BackingField);
                instructions.RemoveAt(index - 2);
                index -= 1;
            }
        }

        private static void ProcessSetPropertyCalls(this MethodDefinition method, AutoPropertyToBackingFieldMap autoPropertyToBackingFieldMap, ILogger logger)
        {
            var instructions = method.Body.Instructions;
            var numberOfCalls = 0;

            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];

                if (!instruction.IsSetPropertyCall())
                    continue;

                numberOfCalls += 1;

                var propertyName = string.Empty;

                if ((instruction.Previous?.Previous?.IsPropertyGetterCall(out propertyName) != true)
                    || !autoPropertyToBackingFieldMap.TryGetValue(propertyName, out var propertyInfo))
                {
                    logger.LogError($"Invalid usage of extension method 'SetProperty()': {numberOfCalls}. call in method {method.FullName}. 'SetProperty()' is only valid on auto-properties of class {method.DeclaringType.Name}.");
                    return;
                }

                logger.LogInfo($"Replace SetProperty() on property {propertyName} in method {method.FullName} with property setter.");

                instructions[index] = Instruction.Create(OpCodes.Call, propertyInfo.Property.SetMethod);
                instructions.RemoveAt(index - 2);
                index -= 1;
            }
        }
    }
}
