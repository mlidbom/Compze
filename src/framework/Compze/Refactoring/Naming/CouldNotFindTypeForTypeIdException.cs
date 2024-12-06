using System;

namespace Compze.Refactoring.Naming;

public class CouldNotFindTypeForTypeIdException(string typeId) : Exception(CreateMessage(typeId))
{
   static string CreateMessage(string typeId) => $"Failed to find a type TypeId: {typeId}";
}