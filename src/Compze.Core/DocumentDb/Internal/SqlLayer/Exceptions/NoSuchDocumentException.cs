using System;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Core.DocumentDb.Internal.SqlLayer.Exceptions;

[SuppressMessage("ReSharper", "MemberCanBeInternal")]
public class NoSuchDocumentException(object key, Type type) : ArgumentOutOfRangeException($"Type: {type.FullName}, Key: {key}");
