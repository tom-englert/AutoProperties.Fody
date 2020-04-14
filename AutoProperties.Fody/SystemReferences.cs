namespace AutoProperties.Fody
{
    using System;
    using System.Reflection;

    using FodyTools;

    using Mono.Cecil;

    internal class SystemReferences
    {
        public SystemReferences(ITypeSystem typeSystem)
        {
#pragma warning disable CS1720 // Expression will always cause a System.NullReferenceException because the type's default value is null
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            GetFieldFromHandle = typeSystem.ImportMethod(() => FieldInfo.GetFieldFromHandle(default));
            PropertyInfoType = typeSystem.ImportType<PropertyInfo>();
            GetTypeFromHandle = typeSystem.ImportMethod(() => Type.GetTypeFromHandle(default));
            GetPropertyInfo = typeSystem.TryImportMethod(() => default(Type).GetProperty(default, default(BindingFlags)));
        }

        public MethodReference GetTypeFromHandle { get; }

        public TypeReference PropertyInfoType { get; }

        public MethodReference GetFieldFromHandle { get; }

        public MethodReference? GetPropertyInfo { get; }
    }
}