using System;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

public class NoSuchDocumentException : ArgumentOutOfRangeException
{
   public NoSuchDocumentException(object key, Type type):base($"Type: {type.FullName}, Key: {key}")
   {
   }

   public NoSuchDocumentException(object key, Guid type) : base($"TypeId.Guid: {type}, Key: {key}")
   {
   }
}