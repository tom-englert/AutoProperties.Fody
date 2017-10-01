using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using Mono.Cecil;

namespace AutoProperties.Fody
{
    internal class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
    {
        private TypeReferenceEqualityComparer()
        {
        }

        [NotNull]
        public static IEqualityComparer<TypeReference> Default { get; } = new TypeReferenceEqualityComparer();

        public bool Equals(TypeReference x, TypeReference y)
        {
            return GetKey(x) == GetKey(y);
        }

        public int GetHashCode([CanBeNull] TypeReference obj)
        {
            return GetKey(obj)?.GetHashCode() ?? 0;
        }

        [CanBeNull]
        private static string GetKey([CanBeNull] TypeReference obj)
        {
            if (obj == null)
                return null;

            return GetAssemblyName(obj.Scope) + "|" + obj.FullName;
        }

        [CanBeNull]
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute"), SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private static string GetAssemblyName([CanBeNull] IMetadataScope scope)
        {
            if (scope == null)
                return null;

            if (scope is ModuleDefinition md)
            {
                return md.Assembly.FullName;
            }

            return scope.ToString();
        }
    }
}