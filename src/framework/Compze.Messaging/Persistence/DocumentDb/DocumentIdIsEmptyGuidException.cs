using System;

namespace Compze.Persistence.DocumentDb;

public class DocumentIdIsEmptyGuidException : Exception
{
   public DocumentIdIsEmptyGuidException():base("It is not allowed to use Guid.Empty as the key for a document.")
   {}
}