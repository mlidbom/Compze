using System;
using System.Collections.Generic;
using Compze.Abstractions.Public;

namespace Compze.Sql.DocumentDb.Abstractions;

public interface IDocumentDbBulkReader : IDocumentDbReader
{
   IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
   IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>;
}