using Compze.Core.Public;

namespace Compze.Core.DocumentDb.Public;

public interface IDocumentDbBulkReader : IDocumentDbReader
{
   IEnumerable<T> GetAll<T>() where T : IEntity<Guid>;
   IEnumerable<Guid> GetAllIds<T>() where T : IEntity<Guid>;
}