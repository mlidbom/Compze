using System;
using System.Collections.Generic;
using Compze.Core.Public;

namespace Compze.Core.DocumentDb.Public;

public interface IDocumentDbBulkReader : IDocumentDbReader
{
   IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
   IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>;
}