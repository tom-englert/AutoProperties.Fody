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
            return x?.Resolve() == y?.Resolve();
        }

        public int GetHashCode([CanBeNull] TypeReference obj)
        {
            return obj?.Resolve()?.GetHashCode() ?? 0;
        }
    }
}