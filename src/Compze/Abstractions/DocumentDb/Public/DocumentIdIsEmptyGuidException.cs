using System;

namespace Compze.Abstractions.DocumentDb.Public;

public class DocumentIdIsEmptyGuidException() : Exception("It is not allowed to use Guid.Empty as the key for a document.");