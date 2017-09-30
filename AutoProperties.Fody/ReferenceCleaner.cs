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
        "AutoProperties.BypassAutoPropertySettersInConstructorsAttribute"
    };

    private static void ProcessAssembly([NotNull] ModuleDefinition moduleDefinition)
    {
        // ReSharper disable once PossibleNullReferenceException
        foreach (var type in moduleDefinition.GetTypes())
        {
            // ReSharper disable once AssignNullToNotNullAttribute
            ProcessType(type);
        }

        // ReSharper disable once PossibleNullReferenceException
        // ReSharper disable once AssignNullToNotNullAttribute
        RemoveAttributes(moduleDefinition.Assembly.CustomAttributes);
    }

    private static void ProcessType([NotNull] TypeDefinition type)
    {
        // ReSharper disable once AssignNullToNotNullAttribute
        RemoveAttributes(type.CustomAttributes);

        // ReSharper disable once PossibleNullReferenceException
        foreach (var property in type.Properties)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            RemoveAttributes(property.CustomAttributes);
        }
        // ReSharper disable once PossibleNullReferenceException
        foreach (var field in type.Fields)
        {
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            RemoveAttributes(field.CustomAttributes);
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

        // ReSharper disable once AssignNullToNotNullAttribute
        // ReSharper disable once PossibleNullReferenceException
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