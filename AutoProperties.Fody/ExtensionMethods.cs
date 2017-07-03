using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoProperties.Fody
{
    internal static class ExtensionMethods
    {
        public static bool? ShouldBypassAutoPropertySettersInConstructors(this ICustomAttributeProvider node)
        {
            return node?
                .CustomAttributes
                .GetAttribute("AutoProperties.BypassAutoPropertySettersInConstructorsAttribute")?
                .ConstructorArguments?
                .Select(arg => arg.Value as bool?)
                .FirstOrDefault();
        }

        private static CustomAttribute GetAttribute(this IEnumerable<CustomAttribute> attributes, string attributeName)
        {
            return attributes.FirstOrDefault(attribute => attribute.Constructor.DeclaringType.FullName == attributeName);
        }

        public static bool IsPropertySetterCall(this Instruction instruction, out string propertyName)
        {
            return IsPropertyCall(instruction, "set_", out propertyName);
        }

        public static bool IsPropertyGetterCall(this Instruction instruction, out string propertyName)
        {
            return IsPropertyCall(instruction, "get_", out propertyName);
        }

        private static bool IsPropertyCall(this Instruction instruction, string prefix, out string propertyName)
        {
            propertyName = null;

            if (instruction.OpCode.Code != Code.Call)
            {
                return false;
            }

            var operand = instruction.Operand as MethodDefinition;
            if (operand == null)
            {
                return false;
            }

            if (!(operand.IsSetter || operand.IsGetter))
            {
                return false;
            }

            var operandName = operand.Name;
            if (!operandName.StartsWith(prefix))
            {
                return false;
            }

            propertyName = operandName.Substring(prefix.Length);
            return true;
        }

        public static FieldDefinition FindAutoPropertyBackingField(this PropertyDefinition property, IEnumerable<FieldDefinition> fields)
        {
            var propertyName = property.Name;

            return fields.FirstOrDefault(field => field.Name == $"<{propertyName}>k__BackingField");
        }

        public static bool IsSetBackingFieldCall(this Instruction instruction)
        {
            return instruction.IsExtensionMethodCall("SetBackingField");
        }

        public static bool IsSetPropertyCall(this Instruction instruction)
        {
            return instruction.IsExtensionMethodCall("SetProperty");
        }

        private static bool IsExtensionMethodCall(this Instruction instruction, string methodName)
        { 
        if (instruction.OpCode.Code != Code.Call)
                return false;

            var operand = instruction.Operand as GenericInstanceMethod;
            if (operand == null)
                return false;

            if (operand.DeclaringType?.FullName != "AutoProperties.BackingFieldAccessExtensions")
                return false;

            if (operand.Name != methodName)
                return false;

            return true;
        }
    }
}
