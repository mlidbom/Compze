using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Core.DocumentDb.Internal.SqlLayer.Exceptions;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public class NoSuchDocumentException : ArgumentOutOfRangeException
{
   public NoSuchDocumentException(object key, Type type) : base($"Type: {type.FullName}, Key: {key}") {}

   public NoSuchDocumentException(object key, Guid type) : base($"TypeId.Guid: {type}, Key: {key}") {}
}
