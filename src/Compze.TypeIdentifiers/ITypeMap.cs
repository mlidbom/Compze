namespace Compze.TypeIdentifiers;

/// <summary>
/// Converts between .NET <see cref="Type"/> objects and their canonical <see cref="TypeId"/> identities,
/// and resolves persisted <c>$type</c> strings back to types. The sole factory for <see cref="TypeId"/>.
/// </summary>
public interface ITypeMap
{
   /// <summary>Returns the canonical <see cref="TypeId"/> for any type — leaf, constructed, or stable.</summary>
   TypeId GetId(Type type);

   /// <summary>Returns the <see cref="TypeId"/> for every mapped leaf type assignable to <paramref name="type"/>.</summary>
   IEnumerable<TypeId> GetIdsForTypesAssignableTo(Type type);

   /// <summary>Throws if any of the given types lack a mapping.</summary>
   void AssertMappingsExistFor(IEnumerable<Type> types);

   /// <summary>
   /// The persisted canonical <c>$type</c> string for a type. Equivalent to <c>GetId(type).CanonicalString</c>.
   /// Mapped leaf types become <c>"GUID, 0"</c>, stable types keep their assembly-qualified names,
   /// and composite types produce a mixed string.
   /// </summary>
   string ToPersistedTypeString(Type type);

   /// <summary>Resolves a persisted canonical <c>$type</c> string back to a .NET <see cref="Type"/>.</summary>
   Type FromPersistedTypeString(string persistedTypeString);

   /// <summary>Resolves a persisted canonical <c>$type</c> string back to its canonical <see cref="TypeId"/>.</summary>
   TypeId GetIdFromPersistedString(string persistedTypeString);
}
