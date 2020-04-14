namespace AutoProperties.Fody
{
    using System.Collections.Generic;

    using Mono.Cecil;

    internal class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
    {
        private TypeReferenceEqualityComparer()
        {
        }

        public static IEqualityComparer<TypeReference> Default { get; } = new TypeReferenceEqualityComparer();

        public bool Equals(TypeReference x, TypeReference y)
        {
            return x?.Resolve() == y?.Resolve();
        }

        public int GetHashCode(TypeReference obj)
        {
            return obj.Resolve()?.GetHashCode() ?? 0;
        }
    }
}