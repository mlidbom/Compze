using System;

namespace Compze.DocumentDb.Abstractions;

public class DocumentIdIsEmptyGuidException() : Exception("It is not allowed to use Guid.Empty as the key for a document.");