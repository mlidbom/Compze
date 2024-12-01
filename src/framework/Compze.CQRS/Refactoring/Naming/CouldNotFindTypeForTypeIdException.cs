using System;

namespace Compze.Refactoring.Naming;

public class CouldNotFindTypeForTypeIdException : Exception
{
   public CouldNotFindTypeForTypeIdException(string typeId) : base(CreateMessage(typeId)) { }
   static string CreateMessage(string typeId) => $"Failed to find a type TypeId: {typeId}";
}