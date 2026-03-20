using System.Diagnostics.CodeAnalysis;

namespace Compze.Abstractions.Refactoring.Naming.Internal;

/// <summary>
/// Maps .NET types to stable identifiers for persistence and serialization.
/// Leaf types get <see cref="MappedTypeId"/> (GUID-backed). Generic and composite types
/// get structural string representations that combine mapped GUIDs with stable assembly-qualified names.
/// </summary>
public interface IStructuralTypeMapper
{
   /// <summary>Returns the <see cref="MappedTypeId"/> for a mapped leaf type.</summary>
   MappedTypeId GetId(Type type);

   /// <summary>Resolves a <see cref="MappedTypeId"/> back to its .NET <see cref="Type"/>.</summary>
   Type GetType(MappedTypeId id);

   /// <summary>Resolves a <see cref="MappedTypeId"/> back to its .NET <see cref="Type"/>, returning false if not found.</summary>
   bool TryGetType(MappedTypeId id, [NotNullWhen(true)] out Type? type);

   /// <summary>Returns the <see cref="MappedTypeId"/> for every mapped leaf type assignable to <paramref name="type"/>.</summary>
   IEnumerable<MappedTypeId> GetIdForTypesAssignableTo(Type type);

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
