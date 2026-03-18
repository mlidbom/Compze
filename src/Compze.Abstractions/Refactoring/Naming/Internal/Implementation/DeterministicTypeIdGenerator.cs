using System.Security.Cryptography;
using System.Text;

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
   internal static readonly TypeId ArrayMarkerTypeId = new(new Guid("b7e3d8f1-6a2c-4e0b-8d5f-1c9a4b3e2d6f"));

   internal static TypeId ComputeCompositeTypeId(TypeId definitionTypeId, params TypeId[] typeArgumentTypeIds)
   {
      var payloadSize = 16 * (1 + typeArgumentTypeIds.Length);
      Span<byte> payload = stackalloc byte[payloadSize];

      definitionTypeId.Value.TryWriteBytes(payload);
      for(var i = 0; i < typeArgumentTypeIds.Length; i++)
      {
         typeArgumentTypeIds[i].Value.TryWriteBytes(payload.Slice(16 * (1 + i)));
      }

      var compositeGuid = CreateUuidV5(CompositionNamespaceId, payload);
      return new TypeId(compositeGuid);
   }

   /// <summary>Computes a stable TypeId from a type's fully qualified name.
   /// Used for external types (e.g. System.Collections.Generic.List`1) whose names are stable.</summary>
   internal static TypeId ComputeTypeIdFromName(string fullyQualifiedTypeName)
   {
      var nameBytes = Encoding.UTF8.GetBytes(fullyQualifiedTypeName);
      var guid = CreateUuidV5(TypeNameNamespaceId, nameBytes);
      return new TypeId(guid);
   }

   // UUID v5 uses SHA1 by specification (RFC 4122), not for cryptographic security.
   // The hash is truncated to 122 bits and used for deterministic GUID generation.
#pragma warning disable CA5350
   static Guid CreateUuidV5(Guid namespaceId, ReadOnlySpan<byte> payload)
#pragma warning restore CA5350
   {
      Span<byte> namespaceBytes = stackalloc byte[16];
      namespaceId.TryWriteBytes(namespaceBytes);
      SwapToNetworkOrder(namespaceBytes);

      Span<byte> hashInput = stackalloc byte[16 + payload.Length];
      namespaceBytes.CopyTo(hashInput);
      payload.CopyTo(hashInput[16..]);

      Span<byte> hash = stackalloc byte[20];
      SHA1.HashData(hashInput, hash);

      hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // version 5
      hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // variant RFC 4122

      Span<byte> result = hash[..16];
      SwapToNetworkOrder(result);
      return new Guid(result);
   }

   // .NET Guid stores the first three components in little-endian order,
   // but UUID v5 (RFC 4122) requires network (big-endian) order for hashing.
   static void SwapToNetworkOrder(Span<byte> guidBytes)
   {
      // Swap bytes of first DWORD (bytes 0-3)
      (guidBytes[0], guidBytes[3]) = (guidBytes[3], guidBytes[0]);
      (guidBytes[1], guidBytes[2]) = (guidBytes[2], guidBytes[1]);
      // Swap bytes of second WORD (bytes 4-5)
      (guidBytes[4], guidBytes[5]) = (guidBytes[5], guidBytes[4]);
      // Swap bytes of third WORD (bytes 6-7)
      (guidBytes[6], guidBytes[7]) = (guidBytes[7], guidBytes[6]);
   }
}
