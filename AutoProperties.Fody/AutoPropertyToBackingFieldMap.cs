#pragma warning disable CCRSI_ContractForNotNull // Element with [NotNull] attribute does not have a corresponding not-null contract.
#pragma warning disable CCRSI_CreateContractInvariantMethod // Missing Contract Invariant Method.

using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using Mono.Cecil;

namespace AutoProperties.Fody
{
    internal class AutoPropertyToBackingFieldMap
    {
        [NotNull]
        private readonly TypeDefinition _classDefinition;
        [CanBeNull]
        private IDictionary<string, AutoPropertyInfo> _map;

        public AutoPropertyToBackingFieldMap([NotNull] TypeDefinition classDefinition)
        {
            _classDefinition = classDefinition;
        }

        [ContractAnnotation("value:notnull => true")]
        public bool TryGetValue([NotNull] string propertyName, [CanBeNull] out AutoPropertyInfo value)
        {
            var map = _map ?? (_map = CreateMap());

            return map.TryGetValue(propertyName, out value);
        }

        [NotNull]
        private IDictionary<string, AutoPropertyInfo> CreateMap()
        {
            var fields = _classDefinition.Fields;
            var properties = _classDefinition.Properties;

            // ReSharper disable AssignNullToNotNullAttribute
            return CreateMap(properties, fields);
            // ReSharper restore AssignNullToNotNullAttribute
        }

        [NotNull]
        private static IDictionary<string, AutoPropertyInfo> CreateMap([NotNull, ItemNotNull] ICollection<PropertyDefinition> properties, [NotNull, ItemNotNull] ICollection<FieldDefinition> fields)
        {
            return properties.Select(property => new AutoPropertyInfo {Property = property, BackingField = property.FindAutoPropertyBackingField(fields)})
                .Where(item => item.BackingField != null)
                // ReSharper disable once PossibleNullReferenceException
                .ToDictionary(item => item.Property.Name);
        }
    }

    internal class AutoPropertyInfo
    {
        public FieldDefinition BackingField { get; set; }

        public PropertyDefinition Property { get; set; }
    }
}
