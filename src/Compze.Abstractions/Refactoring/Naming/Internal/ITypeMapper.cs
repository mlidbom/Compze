using System.Diagnostics.CodeAnalysis;

namespace Compze.Abstractions.Refactoring.Naming.Internal;


/// <summary>
/// Maps types to TypeIds.
/// Whenever we serialize or save an instance of a type anywhere, we use the mapped ID, not the type name.
/// That way one can freely rename types without breaking any persisted data or communication between systems.
/// </summary>
public interface ITypeMapper
{
   //todo: Use static type and indexing trick to improve performance
   TypeId GetId(Type type);
   Type GetType(TypeId teventTypeId);
   bool TryGetType(TypeId typeId, [NotNullWhen(true)]out Type? type);
   IEnumerable<TypeId> GetIdForTypesAssignableTo(Type type);
   Unit AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings);

   /// <summary>
   /// Converts a .NET <see cref="Type"/> to its persisted <c>$type</c> string.
   /// For mapped leaf types: <c>"GUID, 0"</c>.
   /// For stable types: the original <c>AssemblyQualifiedName</c>.
   /// For composite types: a mixed string with some GUID+0 and some AQN components.
   /// </summary>
   string ToPersistedTypeString(Type type);

   /// <summary>
   /// Resolves a persisted <c>$type</c> string back to a .NET <see cref="Type"/>.
   /// Handles both old format (plain GUID) and new structural format.
   /// </summary>
   Type FromPersistedTypeString(string persistedTypeString);
}