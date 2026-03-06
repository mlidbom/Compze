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
   unit AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings);
}