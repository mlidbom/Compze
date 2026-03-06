using Compze.Abstractions.Public;

namespace Compze.DocumentDb.Public;

public interface IDocumentDbBulkReader : IDocumentDbReader
{
   IEnumerable<T> GetAll<T>() where T : IEntity<Guid>;
   IEnumerable<Guid> GetAllIds<T>() where T : IEntity<Guid>;
}