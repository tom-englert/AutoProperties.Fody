namespace AutoProperties.Fody
{
    using System.Collections.Generic;
    using System.Linq;

    using FodyTools;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    internal class ReferenceCleaner
    {
        private static readonly HashSet<string> _attributesToRemove = new HashSet<string>
        {
            AttributeNames.BypassAutoPropertySettersInConstructors,
            AttributeNames.InterceptIgnore,
        };

        private static readonly HashSet<string> _attributesToReplace = new HashSet<string>
        {
            AttributeNames.GetInterceptor,
            AttributeNames.SetInterceptor
        };

        private readonly ModuleDefinition _moduleDefinition;
        private readonly ILogger _logger;
        private readonly Dictionary<string, TypeDefinition> _localAttributeTypes = new Dictionary<string, TypeDefinition>();

        public ReferenceCleaner(ModuleDefinition moduleDefinition, ILogger logger)
        {
            _logger = logger;
            _moduleDefinition = moduleDefinition;
        }

        public void RemoveAttributes()
        {
            foreach (var type in _moduleDefinition.GetTypes())
            {
                ProcessType(type);
            }

            RemoveAttributes(_moduleDefinition.CustomAttributes);
            RemoveAttributes(_moduleDefinition.Assembly.CustomAttributes);
        }

        private void ProcessType(TypeDefinition type)
        {
            RemoveAttributes(type.CustomAttributes);

            foreach (var property in type.Properties)
            {
                RemoveAttributes(property.CustomAttributes);
            }

            foreach (var field in type.Fields)
            {
                RemoveAttributes(field.CustomAttributes);
            }

            foreach (var method in type.Methods)
            {
                RemoveAttributes(method.CustomAttributes);
            }
        }

        private void RemoveAttributes(ICollection<CustomAttribute> customAttributes)
        {
            var attributesToRemove = customAttributes
                .Where(attribute => _attributesToRemove.Contains(attribute.Constructor.DeclaringType.FullName))
                .ToArray();

            foreach (var customAttribute in attributesToRemove.ToList())
            {
                customAttributes.Remove(customAttribute);
            }

            var attributesToReplace = customAttributes
                .Where(attribute => _attributesToReplace.Contains(attribute.Constructor.DeclaringType.FullName))
                .ToArray();

            foreach (var customAttribute in attributesToReplace.ToList())
            {
                customAttributes.Remove(customAttribute);
                customAttributes.Add(GetLocal(customAttribute));
            }
        }

        private CustomAttribute GetLocal(CustomAttribute customAttribute)
        {
            var constructor = customAttribute.Constructor.Resolve();
            var attributeType = constructor.DeclaringType.Resolve();

            if (!_localAttributeTypes.TryGetValue(attributeType.FullName, out var localAttributeType))
            {
                if (attributeType.Module == _moduleDefinition)
                {
                    _logger.LogInfo($"\tAssembly already contains a local attribute {attributeType.FullName}");
                    _localAttributeTypes.Add(attributeType.FullName, attributeType);
                    return customAttribute;
                }

                _logger.LogInfo($"\tAdd local attribute {attributeType.FullName}");
                var baseType = _moduleDefinition.ImportReference(attributeType.BaseType);

                localAttributeType = new TypeDefinition(attributeType.Namespace, attributeType.Name, TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed, baseType);
                var localConstructor = new MethodDefinition(".ctor", constructor.Attributes, _moduleDefinition.TypeSystem.Void)
                {
                    HasThis = constructor.HasThis
                };

                localConstructor.Body.Instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Call, _moduleDefinition.ImportReference(baseType.Resolve().GetConstructors().First(ctor => !ctor.HasParameters))),
                    Instruction.Create(OpCodes.Ret));
                localAttributeType.Methods.Add(localConstructor);
                _moduleDefinition.Types.Add(localAttributeType);
                _localAttributeTypes.Add(attributeType.FullName, localAttributeType);
            }

            return new CustomAttribute(localAttributeType.GetConstructors().First());
        }
    }
}