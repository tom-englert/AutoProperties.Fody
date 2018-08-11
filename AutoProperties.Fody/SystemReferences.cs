using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Fody;

using FodyTools;

using JetBrains.Annotations;

using Mono.Cecil;

namespace AutoProperties.Fody
{
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    internal class SystemReferences
    {
        public SystemReferences([NotNull] BaseModuleWeaver weaver)
        {
            GetFieldFromHandle = weaver.ImportMethod(() => FieldInfo.GetFieldFromHandle(default(RuntimeFieldHandle)));
            PropertyInfoType = weaver.ImportType<PropertyInfo>();
            GetTypeFromHandle = weaver.ImportMethod(() => Type.GetTypeFromHandle(default(RuntimeTypeHandle)));
            GetPropertyInfo = weaver.TryImportMethod(() => default(Type).GetProperty(default(string), default(BindingFlags)));
        }

        [NotNull]
        public MethodReference GetTypeFromHandle { get; }

        [NotNull]
        public TypeReference PropertyInfoType { get; }

        [NotNull]
        public MethodReference GetFieldFromHandle { get; }

        [CanBeNull]
        public MethodReference GetPropertyInfo { get; }
    }
}