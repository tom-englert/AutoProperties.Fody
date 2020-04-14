namespace AutoProperties.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using FodyTools;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    internal static class ExtensionMethods
    {
        public static bool? ShouldBypassAutoPropertySettersInConstructors(this ICustomAttributeProvider? node)
        {
            return node?
                .CustomAttributes
                .GetAttribute(AttributeNames.BypassAutoPropertySettersInConstructors)?
                .ConstructorArguments?
                .Select(arg => arg.Value as bool?)
                .FirstOrDefault();
        }

        public static CustomAttribute? GetAttribute(this IEnumerable<CustomAttribute> attributes, string? attributeName)
        {
            return attributes.FirstOrDefault(attribute => attribute.Constructor?.DeclaringType?.FullName == attributeName);
        }

        public static bool IsPropertySetterCall(this Instruction instruction, [NotNullWhen(true)] out string? propertyName)
        {
            return IsPropertyCall(instruction, "set_", out propertyName);
        }

        public static bool IsPropertyGetterCall(this Instruction instruction, [NotNullWhen(true)] out string? propertyName)
        {
            return IsPropertyCall(instruction, "get_", out propertyName);
        }

        private static bool IsPropertyCall(this Instruction instruction, string prefix, [NotNullWhen(true)] out string? propertyName)
        {
            propertyName = null;

            if (instruction.OpCode.Code != Code.Call)
            {
                return false;
            }

            if (!(instruction.Operand is MethodDefinition operand))
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

        public static FieldDefinition? FindAutoPropertyBackingField(this PropertyDefinition property, IEnumerable<FieldDefinition> fields)
        {
            var propertyName = property.Name;

            return fields.FirstOrDefault(field => field.Name == $"<{propertyName}>k__BackingField");
        }

        public static bool IsExtensionMethodCall(this Instruction? instruction, string? methodName)
        {
            if (instruction?.OpCode.Code != Code.Call)
                return false;

            if (!(instruction.Operand is GenericInstanceMethod operand))
                return false;

            if (operand.DeclaringType?.FullName != "AutoProperties.BackingFieldAccessExtensions")
                return false;

            if (operand.Name != methodName)
                return false;

            return true;
        }

        public static IEnumerable<TypeDefinition> GetSelfAndBaseTypes(this TypeDefinition type)
        {
            yield return type;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            while ((type = type.BaseType?.Resolve()) != null)
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            {
                yield return type;
            }
        }

        public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey? key)
            where TKey: class
            where TValue: class
        {
            if (key is null)
                return default;

            return dictionary.TryGetValue(key, out var value) ? value : default;
        }

        public static bool AccessesMember(this MethodDefinition method, IMemberDefinition member)
        {
            return method.Body?.Instructions?.Any(inst => inst?.Operand == member) ?? false;
        }

        public static void ReplaceFieldAccessWithPropertySetter(this MethodDefinition constructor, IMemberDefinition field, PropertyDefinition property, ISymbolReader? symbolReader)
        {
            var setMethod = property.SetMethod;
            if (setMethod == null)
                return;

            // field initializers are called before the call to the base constructor, but property setters must be called after!

            var instructions = constructor.Body?.Instructions;
            if (instructions == null)
                return;

            var instructionSequences = new InstructionSequences(instructions, constructor.ReadSequencePoints(symbolReader));

            var newInstructions = new List<Instruction>();

            foreach (var sequence in instructionSequences)
            {
                for (var i = 0; i < sequence.Count; i++)
                {
                    var instruction = sequence[i];

                    if ((instruction.OpCode != OpCodes.Stfld) || (instruction.Operand != field) || (i < 2))
                        continue;

                    sequence[i] = Instruction.Create(OpCodes.Call, setMethod);
                    newInstructions.AddRange(sequence.Take(i + 1));

                    for (var k = 0; k <= i; k++)
                    {
                        sequence.RemoveAt(0);
                    }

                    break;
                }
            }

            var index = instructions.TakeWhile(inst => !inst.IsBaseConstructorCall(constructor)).Count() + 1;

            if (index <= instructions.Count)
            {
                instructions.InsertRange(index, newInstructions.ToArray());
            }
        }

        private static bool IsBaseConstructorCall(this Instruction instruction, MethodDefinition constructor)
        {
            return (instruction.OpCode == OpCodes.Call)
                   && (instruction.Operand is MethodReference targetMethod)
                   && (targetMethod.Name == ".ctor")
                   && (targetMethod.DeclaringType.Resolve() == constructor.DeclaringType?.BaseType.Resolve());
        }

        public static FieldReference GetReference(this FieldDefinition field)
        {
            // Make the backing field - even of get-only properties - accessible by the interceptors...
            field.IsInitOnly = false;

            var declaringType = field.DeclaringType;

            if (!declaringType.HasGenericParameters)
                return field;

            var generic = new GenericInstanceType(declaringType);

            generic.GenericArguments.AddRange(declaringType.GenericParameters);

            var genericField = new FieldReference(field.Name, field.FieldType, generic);

            return genericField;
        }

        public static TypeReference GetReference(this TypeReference type)
        {
            return GetReference(type, type.GenericParameters.Cast<TypeReference>().ToArray());
        }

        private static TypeReference GetReference(this TypeReference type, ICollection<TypeReference> arguments)
        {
            if (!type.HasGenericParameters)
                return type;

            if (type.GenericParameters.Count != arguments.Count)
                throw new ArgumentException("Generic parameters mismatch");

            var instance = new GenericInstanceType(type);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        public static MethodReference GetReference(this MethodReference callee, TypeReference callingType)
        {
            var genericParameterProvider = callingType.Module.TryImportReference(callingType.Resolve()?.GetSelfAndBaseTypes().FirstOrDefault(t => t.HasGenericParameters));

            return callingType.Module.ImportReference(InnerGetReference(callee, callingType), genericParameterProvider);
        }

        private static MethodReference InnerGetReference(MethodReference callee, TypeReference callingType)
        {
            var calleeType = callee.DeclaringType.Resolve();

            var baseType = callingType;
            var genericParameters = callingType.GenericParameters.ToArray();
            var genericArguments = genericParameters.Cast<TypeReference>().ToArray();

            while (baseType.Resolve() != calleeType)
            {
                baseType = baseType.Resolve().BaseType;
                if (baseType == null)
                    return callee;

                if (baseType is IGenericInstance genericInstance)
                {
                    var arguments = genericInstance.GenericArguments.ToArray();

                    for (var i = 0; i < arguments.Length; i++)
                    {
                        var argument = arguments[i];

                        if (!argument.ContainsGenericParameter)
                            continue;

                        var position = ((GenericParameter)argument).Position;

                        if (genericParameters.Length > position)
                        {
                            arguments[i] = genericArguments[position];
                        }
                    }

                    genericArguments = arguments;
                    genericParameters = baseType.GetElementType().GenericParameters.ToArray();
                }
            }

            if (!genericArguments.Any())
                return callee;

            var reference = new MethodReference(callee.Name, callee.ReturnType)
            {
                DeclaringType = callee.DeclaringType.GetReference(genericArguments.ToArray()),
                HasThis = callee.HasThis,
                ExplicitThis = callee.ExplicitThis,
                CallingConvention = callee.CallingConvention,
            };

            reference.Parameters.AddRange(callee.Parameters.Select(parameter => new ParameterDefinition(parameter.ParameterType)));
            reference.GenericParameters.AddRange(callee.GenericParameters.Select(parameter => new GenericParameter(parameter.Name, reference)));

            return reference;
        }

        public static MethodReference? TryImportReference(this ModuleDefinition module, MethodReference? method)
        {
            return method == null ? null : module.ImportReference(method);
        }

        public static TypeReference? TryImportReference(this ModuleDefinition module, TypeReference? type)
        {
            return type == null ? null : module.ImportReference(type);
        }
    }
}
