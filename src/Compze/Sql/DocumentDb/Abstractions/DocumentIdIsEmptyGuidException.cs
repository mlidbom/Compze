using System;

namespace Compze.Sql.DocumentDb.Abstractions;

public class DocumentIdIsEmptyGuidException() : Exception("It is not allowed to use Guid.Empty as the key for a document.");