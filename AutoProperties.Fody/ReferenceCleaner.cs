// ReSharper disable PossibleNullReferenceException
// ReSharper disable AssignNullToNotNullAttribute

using System.Collections.Generic;
using System.Linq;

using AutoProperties.Fody;

using JetBrains.Annotations;

using Mono.Cecil;

internal static class ReferenceCleaner
{
    [NotNull, ItemNotNull]
    private static readonly HashSet<string> _attributeNames = new HashSet<string>
    {
        "AutoProperties.BypassAutoPropertySettersInConstructorsAttribute",
        "AutoProperties.GetInterceptorAttribute",
        "AutoProperties.SetInterceptorAttribute",
        "AutoProperties.InterceptIgnoreAttribute"
    };

    private static void ProcessAssembly([NotNull] ModuleDefinition moduleDefinition)
    {
        foreach (var type in moduleDefinition.GetTypes())
        {
            ProcessType(type);
        }

        RemoveAttributes(moduleDefinition.CustomAttributes);
        RemoveAttributes(moduleDefinition.Assembly.CustomAttributes);
    }

    private static void ProcessType([NotNull] TypeDefinition type)
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

    private static void RemoveAttributes([NotNull, ItemNotNull] ICollection<CustomAttribute> customAttributes)
    {
        var attributes = customAttributes
            .Where(attribute => _attributeNames.Contains(attribute.Constructor?.DeclaringType?.FullName))
            .ToArray();

        foreach (var customAttribute in attributes.ToList())
        {
            customAttributes.Remove(customAttribute);
        }
    }

    public static void RemoveReferences([NotNull] this ModuleDefinition moduleDefinition, [NotNull] ILogger logger)
    {
        ProcessAssembly(moduleDefinition);

        var referenceToRemove = moduleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "AutoProperties");
        if (referenceToRemove == null)
        {
            logger.LogInfo("\tNo reference to 'AutoProperties' found. References not modified.");
            return;
        }

        logger.LogInfo("\tRemoving reference to 'AutoProperties'.");
        if (!moduleDefinition.AssemblyReferences.Remove(referenceToRemove))
        {
            logger.LogWarning("\tCould not remove all references to 'AutoProperties'.");
        }
    }
}