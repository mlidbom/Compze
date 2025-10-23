using System;

namespace Compze.Sql.DocumentDb.Abstractions.Internal;

class NoSuchDocumentException : ArgumentOutOfRangeException
{
   public NoSuchDocumentException(object key, Type type):base($"Type: {type.FullName}, Key: {key}")
   {
   }

   public NoSuchDocumentException(object key, Guid type) : base($"TypeId.Guid: {type}, Key: {key}")
   {
   }
}