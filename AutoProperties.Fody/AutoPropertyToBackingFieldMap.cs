namespace AutoProperties.Fody
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Mono.Cecil;

    internal class AutoPropertyToBackingFieldMap
    {
        private readonly TypeDefinition _classDefinition;
        private IDictionary<string, AutoPropertyInfo>? _map;

        public AutoPropertyToBackingFieldMap(TypeDefinition classDefinition)
        {
            _classDefinition = classDefinition;
        }

        public bool TryGetValue(string propertyName, [NotNullWhen(true)] out AutoPropertyInfo? value)
        {
            var map = _map ??= CreateMap();

            return map.TryGetValue(propertyName, out value);
        }

        private IDictionary<string, AutoPropertyInfo> CreateMap()
        {
            var fields = _classDefinition.Fields;
            var properties = _classDefinition.Properties;

            return CreateMap(properties, fields);
        }

        private static IDictionary<string, AutoPropertyInfo> CreateMap(ICollection<PropertyDefinition> properties, ICollection<FieldDefinition> fields)
        {
            return properties.Select(property => new { Property = property, BackingField = property.FindAutoPropertyBackingField(fields) })
                .Where(item => item.BackingField != null)
                .Select(item => new AutoPropertyInfo(item.BackingField!, item.Property))
                .ToDictionary(item => item.Property.Name);
        }
    }

    internal class AutoPropertyInfo
    {
        public AutoPropertyInfo(FieldDefinition backingField, PropertyDefinition property)
        {
            BackingField = backingField;
            Property = property;
        }

        public FieldDefinition BackingField { get; }

        public PropertyDefinition Property { get; }
    }
}
