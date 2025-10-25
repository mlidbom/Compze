using System;

namespace Compze.Core.DocumentDb.Public;

public class DocumentIdIsEmptyGuidException() : Exception("It is not allowed to use Guid.Empty as the key for a document.");