namespace Compze.TypeIdentifiers.Interning;

/// <summary>
/// Interns a <see cref="TypeId"/> to a small, database-local integer id and back, so storage tables can
/// reference a type with a 4-byte int instead of a GUID or a long string.
/// <para>
/// The integer is purely a storage optimization and is meaningful ONLY within the single database that
/// assigned it. It must never cross a database boundary — the portable identity is the <see cref="TypeId"/>'s
/// canonical string, which is what actually gets persisted in the <c>TypeIds</c> table.
/// </para>
/// </summary>
public interface ITypeIdInterner
{
   /// <summary>Returns the interned id for <paramref name="typeId"/>, assigning and persisting a new one if it has never been seen.</summary>
   int GetOrInternId(TypeId typeId);

   /// <summary>
   /// Returns the interned id for <paramref name="typeId"/> if it has already been interned, otherwise <c>false</c>.
   /// A type that was never interned cannot be referenced by any stored row, so callers treat a <c>false</c>
   /// result as "nothing stored can match this type".
   /// </summary>
   bool TryGetInternedId(TypeId typeId, out int internedId);

   /// <summary>Resolves an interned id back to its canonical <see cref="TypeId"/>.</summary>
   TypeId GetTypeId(int internedId);
}
