// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

using System.Collections.Generic;
using System.Linq;

using AutoProperties.Fody;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

internal class ReferenceCleaner
{
    [NotNull, ItemNotNull]
    private static readonly HashSet<string> _attributesToRemove = new HashSet<string>
    {
        AttributeNames.BypassAutoPropertySettersInConstructors,
        AttributeNames.InterceptIgnore,
    };

    [NotNull, ItemNotNull]
    private static readonly HashSet<string> _attributesToReplace = new HashSet<string>
    {
        AttributeNames.GetInterceptor,
        AttributeNames.SetInterceptor
    };

    [NotNull]
    private readonly ModuleDefinition _moduleDefinition;
    [NotNull]
    private readonly ILogger _logger;
    [NotNull]
    private readonly Dictionary<string, TypeDefinition> _localAttributeTypes = new Dictionary<string, TypeDefinition>();

    public ReferenceCleaner([NotNull] ModuleDefinition moduleDefinition, [NotNull] ILogger logger)
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

    private void ProcessType([NotNull] TypeDefinition type)
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

    private void RemoveAttributes([NotNull, ItemNotNull] ICollection<CustomAttribute> customAttributes)
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

    [NotNull]
    private CustomAttribute GetLocal([NotNull] CustomAttribute customAttribute)
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

    public void RemoveReferences()
    {
        var referenceToRemove = _moduleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "AutoProperties");
        if (referenceToRemove == null)
        {
            _logger.LogInfo("\tNo reference to 'AutoProperties' found. References not modified.");
            return;
        }

        _logger.LogInfo("\tRemoving reference to 'AutoProperties'.");
        if (!_moduleDefinition.AssemblyReferences.Remove(referenceToRemove))
        {
            _logger.LogWarning("\tCould not remove all references to 'AutoProperties'.");
        }
    }
}