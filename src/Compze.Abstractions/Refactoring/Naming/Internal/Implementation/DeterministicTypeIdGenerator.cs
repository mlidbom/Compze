using System.Text;
using Compze.Contracts;
using Compze.Internals.SystemCE;

namespace Compze.Abstractions.Refactoring.Naming.Internal.Implementation;

static class DeterministicTypeIdGenerator
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

    internal static TypeId GenerateArrayTypeId(params TypeId[] typeArgumentTypeIds) => Generate(ArrayMarkerTypeId, typeArgumentTypeIds);
    internal static TypeId Generate(TypeId definitionTypeId, params TypeId[] typeArgumentTypeIds)
    {
        var payloadSize = 16 * (1 + typeArgumentTypeIds.Length);
        Span<byte> payload = stackalloc byte[payloadSize];

        definitionTypeId.Value.TryWriteBytes(payload)._assert().True();
        for (var i = 0; i < typeArgumentTypeIds.Length; i++)
        {
            typeArgumentTypeIds[i].Value.TryWriteBytes(payload.Slice(16 * (1 + i)))._assert().True();
        }

        return new TypeId(CompositionNamespaceId.CreateUuidV5(payload));
    }

    /// <summary>Computes a stable TypeId from a type's fully qualified name.
    /// Used for external types (e.g. System.Collections.Generic.List`1) whose names are stable.</summary>
    internal static TypeId ComputeTypeIdFromName(string fullyQualifiedTypeName)
    {
        var nameBytes = Encoding.UTF8.GetBytes(fullyQualifiedTypeName);
        return new TypeId(TypeNameNamespaceId.CreateUuidV5(nameBytes));
    }
}
