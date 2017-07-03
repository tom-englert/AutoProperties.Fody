using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;

namespace AutoProperties.Fody
{
    internal class AutoPropertyToBackingFieldMap
    {
        private readonly TypeDefinition _classDefinition;
        private IDictionary<string, AutoPropertyInfo> _map;

        public AutoPropertyToBackingFieldMap(TypeDefinition classDefinition)
        {
            _classDefinition = classDefinition;
        }

        public bool TryGetValue(string propertyName, out AutoPropertyInfo value)
        {
            var map = _map ?? (_map = CreateMap());

            return map.TryGetValue(propertyName, out value);
        }

        private IDictionary<string, AutoPropertyInfo> CreateMap()
        {
            var fields = _classDefinition.Fields;
            var properties = _classDefinition.Properties;

            return properties.Select(property => new AutoPropertyInfo { Property = property, BackingField = property.FindAutoPropertyBackingField(fields) })
                .Where(item => item.BackingField != null)
                .ToDictionary(item => item.Property.Name);
        }
    }

    internal class AutoPropertyInfo
    {
        public FieldDefinition BackingField { get; set; }

        public PropertyDefinition Property { get; set; }
    }
}
