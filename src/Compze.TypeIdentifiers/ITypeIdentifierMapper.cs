using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Compze.TypeIdentifiers;

/// <summary>
/// Maps .NET types to stable identifiers for persistence and serialization.
/// Leaf types get <see cref="MappedTypeIdentifier"/> (GUID-backed). Generic and composite types
/// get structural string representations that combine mapped GUIDs with stable assembly-qualified names.
/// Supports incremental assembly registration.
/// </summary>
public interface ITypeMapper
{
   /// <summary>
   /// Register type mappings from the assembly containing <typeparamref name="T"/>.
   /// The assembly must have a <see cref="TypeMappingsAttribute"/> with an <see cref="ITypeMappingDeclaration"/>.
   /// </summary>
   void MapTypesFromAssemblyContaining<T>();

   /// <summary>Register type mappings from the specified assembly.</summary>
   void MapTypesFromAssembly(Assembly assembly);

   /// <summary>Register the assembly containing <typeparamref name="T"/> as stable (type names pass through unchanged).</summary>
   void UseStableNameStrategyForAssemblyContaining<T>();

   /// <summary>Returns the <see cref="TypeIdentifier"/> for any type — leaf, constructed, or stable.</summary>
   TypeIdentifier GetId(Type type);

   /// <summary>Resolves any <see cref="TypeIdentifier"/> subtype back to its .NET <see cref="Type"/>.</summary>
   Type GetType(TypeIdentifier id);

   /// <summary>Resolves any <see cref="TypeIdentifier"/> subtype back to its .NET <see cref="Type"/>, returning false if not found.</summary>
   bool TryGetType(TypeIdentifier id, [NotNullWhen(true)] out Type? type);

   /// <summary>Returns the <see cref="MappedTypeIdentifier"/> for a mapped leaf type. Use for SQL GUID column storage only.</summary>
   MappedTypeIdentifier GetMappedId(Type type);

   /// <summary>Resolves a <see cref="MappedTypeIdentifier"/> back to its .NET <see cref="Type"/>. Use for SQL GUID column storage only.</summary>
   Type GetType(MappedTypeIdentifier id);

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
