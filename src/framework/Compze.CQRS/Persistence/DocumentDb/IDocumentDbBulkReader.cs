using System;
using System.Collections.Generic;
using Compze.DDD;

namespace Compze.Persistence.DocumentDb;

public interface IDocumentDbBulkReader : IDocumentDbReader
{
   IEnumerable<T> GetAll<T>() where T : IHasPersistentIdentity<Guid>;
   IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>;
}