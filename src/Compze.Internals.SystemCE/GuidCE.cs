using System.Security.Cryptography;
using System.Text;

namespace Compze.Internals.SystemCE;

/// <summary>Contains utility methods for <see cref="Guid"/>.</summary>
public static class GuidCE
{
   extension(Guid)
   {
      /// <summary>Generates a deterministic UUID v5 (RFC 4122 / RFC 9562 §5.5) from a namespace GUID and a UTF-8 encoded name string.
      /// The same inputs always produce the same output GUID.</summary>
      public static Guid NewUUIDv5(Guid namespaceId, string name)
         => NewUUIDv5(namespaceId, Encoding.UTF8.GetBytes(name));

      /// <summary>Generates a deterministic UUID v5 (RFC 4122 / RFC 9562 §5.5) from a namespace GUID
      /// and an ordered list of component GUIDs serialized as their raw 16-byte representations.</summary>
      public static Guid NewUUIDv5(Guid namespaceId, IReadOnlyList<Guid> components)
      {
         Span<byte> payload = stackalloc byte[16 * components.Count];
         for(var i = 0; i < components.Count; i++)
            components[i].TryWriteBytes(payload.Slice(16 * i));
         return NewUUIDv5(namespaceId, payload);
      }

      /// <summary>Generates a deterministic UUID v5 (RFC 4122 / RFC 9562 §5.5) from a namespace GUID and a byte payload.
      /// The same inputs always produce the same output GUID.</summary>
#pragma warning disable CA5350 // Do not use weak encryption : SHA-1 is mandated by the UUID v5 specification, not used for security
      public static Guid NewUUIDv5(Guid namespaceId, ReadOnlySpan<byte> payload)
      {
         Span<byte> namespaceBytes = stackalloc byte[16];
         namespaceId.TryWriteBytes(namespaceBytes);
         SwapGuidBytesToNetworkOrder(namespaceBytes);

         Span<byte> hashInput = stackalloc byte[16 + payload.Length];
         namespaceBytes.CopyTo(hashInput);
         payload.CopyTo(hashInput[16..]);

         Span<byte> hash = stackalloc byte[20];
         SHA1.HashData(hashInput, hash);

         hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // version 5
         hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // variant RFC 4122

         Span<byte> result = hash[..16];
         SwapGuidBytesToNetworkOrder(result);
         return new Guid(result);
      }
#pragma warning restore CA5350

      // .NET Guid stores the first three components in little-endian order,
      // but UUID v5 (RFC 4122) requires network (big-endian) order for hashing.
      static void SwapGuidBytesToNetworkOrder(Span<byte> guidBytes)
      {
         (guidBytes[0], guidBytes[3]) = (guidBytes[3], guidBytes[0]);
         (guidBytes[1], guidBytes[2]) = (guidBytes[2], guidBytes[1]);
         (guidBytes[4], guidBytes[5]) = (guidBytes[5], guidBytes[4]);
         (guidBytes[6], guidBytes[7]) = (guidBytes[7], guidBytes[6]);
      }
   }
}
