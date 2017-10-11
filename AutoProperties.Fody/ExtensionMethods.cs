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

        [CanBeNull]
        public static SequencePoint GetEntryPoint([CanBeNull] this MethodDefinition method, [CanBeNull] ISymbolReader symbolReader)
        {
            if (method == null)
                return null;

            return symbolReader?.Read(method)?.SequencePoints?.FirstOrDefault();
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
            if (ReferenceEquals(key, null))
                return default(TValue);

            return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
        }

        public static void AddRange<T>([NotNull, ItemCanBeNull] this IList<T> collection, [NotNull, ItemCanBeNull] params T[] values)
        {
            AddRange(collection, (IEnumerable<T>)values);
        }

        public static void AddRange<T>([NotNull, ItemCanBeNull] this IList<T> collection, [NotNull, ItemCanBeNull] IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                collection.Add(value);
            }
        }

        public static void InsertRange<T>([NotNull, ItemCanBeNull] this IList<T> collection, int index, [NotNull, ItemCanBeNull] params T[] values)
        {
            foreach (var value in values)
            {
                collection.Insert(index++, value);
            }
        }

        public static void Replace<T>([CanBeNull, ItemCanBeNull] this IList<T> collection, [CanBeNull, ItemCanBeNull] IEnumerable<T> values)
        {
            if ((collection == null) || (values == null))
                return;

            collection.Clear();
            collection.AddRange(values);
        }

        public static bool AccessesMember([NotNull] this MethodDefinition method, [NotNull] IMemberDefinition member)
        {
            return method.Body?.Instructions?.Any(inst => inst?.Operand == member) ?? false;
        }

        [NotNull]
        public static FieldReference GetReference([NotNull] this FieldDefinition field)
        {
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
        public static TypeReference GetReference([NotNull] this TypeReference type, [NotNull, ItemNotNull] ICollection<TypeReference> arguments)
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
            return callingType.Module.ImportReference(InnerGetReference(callee, callingType));
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

                    if (genericParameters != null)
                    {
                        for (var i = 0; i < arguments.Count(); i++)
                        {
                            var argument = arguments[i];

                            if (!argument.ContainsGenericParameter)
                                continue;

                            for (var k = 0; k < genericParameters.Count(); k++)
                            {
                                if (genericParameters[k].Name != argument.Name)
                                    continue;

                                arguments[i] = genericArguments[k];
                                break;
                            }
                        }
                    }

                    genericArguments = arguments;
                    genericParameters = baseType.GetElementType().GenericParameters.ToArray();
                }

                if (baseType.Resolve() == calleeType)
                    break;
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
    }
}
