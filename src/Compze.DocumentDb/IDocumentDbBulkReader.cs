using Compze.Abstractions;

namespace Compze.DocumentDb;

public interface IDocumentDbBulkReader : IDocumentDbReader
{
   IEnumerable<T> GetAll<T>() where T : IEntity<Guid>;
   IEnumerable<Guid> GetAllIds<T>() where T : IEntity<Guid>;
}