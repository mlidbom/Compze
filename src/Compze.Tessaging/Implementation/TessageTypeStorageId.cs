using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Implementation;

/// <summary>
/// Storage helper: extracts the GUID a tessage type column persists from a <see cref="TypeId"/>.
/// GUID-column storage is a persistence concern, not the type-identity library's — a mapped leaf type's
/// canonical id is <c>"&lt;guid&gt;, 0"</c>, and only such types can be stored in a GUID type column.
/// </summary>
static class TessageTypeStorageId
{
   internal static Guid LeafStorageGuid(this TypeId id)
   {
      var canonical = id.CanonicalString;
      if(canonical.EndsWith(", 0", StringComparison.Ordinal) && Guid.TryParse(canonical[..^3], out var guid))
         return guid;
      throw new InvalidOperationException(
         $"Tessage type '{id.Type.FullName}' is not a GUID-mapped leaf type (id '{canonical}'); only mapped leaf types can be stored.");
   }
}
