namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

static class DeterministicTypeId
{
    // Fixed namespace GUID for all composite TypeId computations (generics + arrays).
    // This value must NEVER change — persisted data depends on it.
    static readonly Guid CompositionNamespaceId = new("e4a8c9f2-7b3d-4f1a-9c6e-2d8b5a0f3e7c");

    // Fixed namespace GUID for computing TypeIds from stable type names (system types).
    // This value must NEVER change — persisted data depends on it.
    static readonly Guid TypeNameNamespaceId = new("c3d2e1f0-9a8b-4c7d-6e5f-0a1b2c3d4e5f");

    // Synthetic marker used as the "definition" TypeId for array types,
    // since .NET arrays are not generic types (typeof(int[]).IsGenericType == false).
    // This value must NEVER change — persisted data depends on it.
    static readonly TypeId ArrayMarkerTypeId = new(new Guid("b7e3d8f1-6a2c-4e0b-8d5f-1c9a4b3e2d6f"));

    internal static TypeId ForArrayType(TypeId elementTypeId, int rank)
    {
        return new TypeId(Guid.NewUUIDv5(namespaceId: ArrayMarkerTypeId.Value,
                                         components: [elementTypeId.Value, Guid.NewUUIDv5(namespaceId: ArrayMarkerTypeId.Value, name: rank.ToStringInvariant())]));
    }
    internal static TypeId ForClosedGenericType(TypeId openGenericTypeId, params TypeId[] typeArgumentTypeIds)
    {
        var componentGuids = typeArgumentTypeIds.Prepend(openGenericTypeId).Select(it => it.Value).ToList();
        return new TypeId(Guid.NewUUIDv5(namespaceId: CompositionNamespaceId, components: componentGuids));
    }

    /// <summary>Computes a stable TypeId from a type's fully qualified name.
    /// Used for external types (e.g. System.Collections.Generic.List`1) whose names are stable.</summary>
    internal static TypeId ComputeTypeIdFromName(string fullyQualifiedTypeName)
    {
        return new TypeId(Guid.NewUUIDv5(namespaceId: TypeNameNamespaceId, name: fullyQualifiedTypeName));
    }
}
