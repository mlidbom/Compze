using System;

namespace Compze.Persistence.DocumentDb;

class NoSuchDocumentException : Exception
{
   internal NoSuchDocumentException(object key, Type type):base($"Type: {type.FullName}, Key: {key}")
   {
   }

   internal NoSuchDocumentException(object key, Guid type) : base($"TypeId.Guid: {type}, Key: {key}")
   {
   }
}