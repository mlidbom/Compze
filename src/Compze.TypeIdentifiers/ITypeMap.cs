using System.Diagnostics.CodeAnalysis;

namespace Compze.TypeIdentifiers;

public interface ITypeMap
{
   /// <summary>Returns the <see cref="TypeIdentifier"/> for any type — leaf, constructed, or stable.</summary>
   TypeIdentifier GetId(Type type);

   /// <summary>Resolves any <see cref="TypeIdentifier"/> subtype back to its .NET <see cref="Type"/>.</summary>
   Type GetType(TypeIdentifier id);

   /// <summary>Resolves a <see cref="MappedTypeIdentifier"/> back to its .NET <see cref="Type"/>. Use for SQL GUID column storage only.</summary>
   Type GetType(MappedTypeIdentifier id);

   /// <summary>Resolves any <see cref="TypeIdentifier"/> subtype back to its .NET <see cref="Type"/>, returning false if not found.</summary>
   bool TryGetType(TypeIdentifier id, [NotNullWhen(true)] out Type? type);

   /// <summary>Returns the <see cref="MappedTypeIdentifier"/> for a mapped leaf type. Use for SQL GUID column storage only.</summary>
   MappedTypeIdentifier GetMappedId(Type type);

   /// <summary>Returns the <see cref="MappedTypeIdentifier"/> for every mapped leaf type assignable to <paramref name="type"/>.</summary>
   IEnumerable<MappedTypeIdentifier> GetIdForTypesAssignableTo(Type type);

   /// <summary>Throws if any of the given types lack a mapping.</summary>
   void AssertMappingsExistFor(IEnumerable<Type> types);

   /// <summary>
   /// Converts a .NET <see cref="Type"/> to its persisted <c>$type</c> string.
   /// Mapped leaf types become <c>"GUID, 0"</c>, stable types keep their <c>AssemblyQualifiedName</c>,
   /// and composite types produce a mixed string.
   /// </summary>
   string ToPersistedTypeString(Type type);

   /// <summary>
   /// Resolves a persisted <c>$type</c> string back to a .NET <see cref="Type"/>.
   /// </summary>
   Type FromPersistedTypeString(string persistedTypeString);
}
