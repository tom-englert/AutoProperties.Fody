using System;
using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AutoProperties.Fody
{
    internal static class ExtensionMethods
    {
        public static bool? ShouldBypassAutoPropertySettersInConstructors([CanBeNull] this ICustomAttributeProvider node)
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            return node?
                .CustomAttributes
                .GetAttribute("AutoProperties.BypassAutoPropertySettersInConstructorsAttribute")?
                .ConstructorArguments?
                .Select(arg => arg.Value as bool?)
                .FirstOrDefault();
        }

        [CanBeNull]
        private static CustomAttribute GetAttribute([NotNull, ItemNotNull] this IEnumerable<CustomAttribute> attributes, [CanBeNull] string attributeName)
        {
            return attributes.FirstOrDefault(attribute => attribute.Constructor?.DeclaringType?.FullName == attributeName);
        }

        [ContractAnnotation("propertyName:null => false")]
        public static bool IsPropertySetterCall([NotNull] this Instruction instruction, [CanBeNull] out string propertyName)
        {
            return IsPropertyCall(instruction, "set_", out propertyName);
        }

        [ContractAnnotation("propertyName:null => false")]
        public static bool IsPropertyGetterCall([NotNull] this Instruction instruction, [CanBeNull] out string propertyName)
        {
            return IsPropertyCall(instruction, "get_", out propertyName);
        }

        [ContractAnnotation("propertyName:null => false")]
        private static bool IsPropertyCall([NotNull] this Instruction instruction, [NotNull] string prefix, [CanBeNull] out string propertyName)
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
            if (operandName?.StartsWith(prefix, StringComparison.Ordinal) != true)
            {
                return false;
            }

            propertyName = operandName.Substring(prefix.Length);
            return true;
        }

        [CanBeNull]
        public static FieldDefinition FindAutoPropertyBackingField([NotNull] this PropertyDefinition property, [NotNull, ItemNotNull] IEnumerable<FieldDefinition> fields)
        {
            var propertyName = property.Name;

            return fields.FirstOrDefault(field => field.Name == $"<{propertyName}>k__BackingField");
        }

        [ContractAnnotation("instruction:null => false")]
        public static bool IsExtensionMethodCall([CanBeNull] this Instruction instruction, [CanBeNull] string methodName)
        {
            if (instruction?.OpCode.Code != Code.Call)
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
