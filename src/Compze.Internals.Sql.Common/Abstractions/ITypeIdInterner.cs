namespace Compze.Internals.Sql.Common.Abstractions;

/// <summary>
/// Interns canonical <c>TypeId</c> strings to small, database-local integer ids and back, so storage tables
/// can reference a type with a 4-byte int instead of a GUID or a long string.
/// <para>
/// The integer is purely a storage optimization and is meaningful ONLY within the single database that
/// assigned it. It must never cross a database boundary — the portable identity is always the canonical
/// string. Resolution to a <c>Type</c> still happens through the type map from that string.
/// </para>
/// </summary>
public interface ITypeIdInterner
{
   /// <summary>Returns the interned id for <paramref name="canonicalTypeString"/>, assigning and persisting a new one if it has never been seen.</summary>
   int GetOrInternId(string canonicalTypeString);

   /// <summary>
   /// Returns the interned ids for those of <paramref name="canonicalTypeStrings"/> that have already been
   /// interned. Strings that were never interned are skipped — no document/event can reference an id that was
   /// never assigned, so they cannot match anything stored.
   /// </summary>
   IReadOnlySet<int> GetExistingIds(IEnumerable<string> canonicalTypeStrings);

   /// <summary>Resolves an interned id back to its canonical <c>TypeId</c> string.</summary>
   string GetCanonicalString(int internedId);
}
