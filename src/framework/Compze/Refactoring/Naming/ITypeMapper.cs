using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Refactoring.Naming;

interface ITypeMapper
{
   //todo: Use static type and indexing trick to improve performance
   TypeId GetId(Type type);
   Type GetType(TypeId eventTypeId);
   bool TryGetType(TypeId typeId, [NotNullWhen(true)]out Type? type);
   IEnumerable<TypeId> GetIdForTypesAssignableTo(Type type);
   void AssertMappingsExistFor(IEnumerable<Type> typesThatRequireMappings);
}

public interface ITypeMappingRegistar
{
   ITypeMappingRegistar Map<TType>(Guid typeGuid);
   ITypeMappingRegistar Map<TType>(string typeGuid);
}