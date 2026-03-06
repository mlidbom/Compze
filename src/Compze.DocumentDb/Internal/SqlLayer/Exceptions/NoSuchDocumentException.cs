using System.Diagnostics.CodeAnalysis;

namespace Compze.DocumentDb.Internal.SqlLayer.Exceptions;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public class NoSuchDocumentException(object key, Type type) : ArgumentOutOfRangeException($"Type: {type.FullName}, Key: {key}");
