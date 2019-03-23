using System;
using System.Collections.Generic;
using System.Linq;

using FodyTools;

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
                .GetAttribute(AttributeNames.BypassAutoPropertySettersInConstructors)?
                .ConstructorArguments?
                .Select(arg => arg.Value as bool?)
                .FirstOrDefault();
        }

        [CanBeNull]
        public static CustomAttribute GetAttribute([NotNull, ItemNotNull] this IEnumerable<CustomAttribute> attributes, [CanBeNull] string attributeName)
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

            if (!(instruction.Operand is GenericInstanceMethod operand))
                return false;

            if (operand.DeclaringType?.FullName != "AutoProperties.BackingFieldAccessExtensions")
                return false;

            if (operand.Name != methodName)
                return false;

            return true;
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<TypeDefinition> GetSelfAndBaseTypes([NotNull] this TypeDefinition type)
        {
            yield return type;

            while ((type = type.BaseType?.Resolve()) != null)
            {
                yield return type;
            }
        }

        [CanBeNull]
        public static TValue GetValueOrDefault<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, [CanBeNull] TKey key)
        {
#pragma warning disable IDE0041 // Use 'is null' check
            if (ReferenceEquals(key, null))
#pragma warning restore IDE0041 // Use 'is null' check
                return default(TValue);

            return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
        }

        public static bool AccessesMember([NotNull] this MethodDefinition method, [NotNull] IMemberDefinition member)
        {
            return method.Body?.Instructions?.Any(inst => inst?.Operand == member) ?? false;
        }

        public static void ReplaceFieldAccessWithPropertySetter([NotNull] this MethodDefinition constructor, [NotNull] IMemberDefinition field, [NotNull] PropertyDefinition property, [CanBeNull] ISymbolReader symbolReader)
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

        private static bool IsBaseConstructorCall([NotNull] this Instruction instruction, [NotNull] MethodDefinition constructor)
        {
            return (instruction.OpCode == OpCodes.Call)
                   && (instruction.Operand is MethodReference targetMethod)
                   && (targetMethod.Name == ".ctor")
                   && (targetMethod.DeclaringType.Resolve() == constructor.DeclaringType?.BaseType.Resolve());
        }

        [NotNull]
        public static FieldReference GetReference([NotNull] this FieldDefinition field)
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

        [NotNull]
        public static TypeReference GetReference([NotNull] this TypeReference type)
        {
            return GetReference(type, type.GenericParameters.Cast<TypeReference>().ToArray());
        }

        [NotNull]
        private static TypeReference GetReference([NotNull] this TypeReference type, [NotNull, ItemNotNull] ICollection<TypeReference> arguments)
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

        [NotNull]
        public static MethodReference GetReference([NotNull] this MethodReference callee, [NotNull] TypeReference callingType)
        {
            var genericParameterProvider = callingType.Module.TryImportReference(callingType.Resolve()?.GetSelfAndBaseTypes().FirstOrDefault(t => t.HasGenericParameters));

            return callingType.Module.ImportReference(InnerGetReference(callee, callingType), genericParameterProvider);
        }

        [NotNull]
        private static MethodReference InnerGetReference([NotNull] MethodReference callee, [NotNull] TypeReference callingType)
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

        [CanBeNull]
        public static MethodReference TryImportReference([NotNull] this ModuleDefinition module, [CanBeNull] MethodReference method)
        {
            return method == null ? null : module.ImportReference(method);
        }

        [CanBeNull]
        public static TypeReference TryImportReference([NotNull] this ModuleDefinition module, [CanBeNull] TypeReference type)
        {
            return type == null ? null : module.ImportReference(type);
        }
    }
}
