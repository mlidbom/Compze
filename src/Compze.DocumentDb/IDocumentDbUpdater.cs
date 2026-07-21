using Compze.Abstractions;

namespace Compze.DocumentDb;

public interface IDocumentDbUpdater
{
   /// <summary>Like Get but, if supported by implementing class, eagerly locks the instance in the database.</summary>
   TValue GetForUpdate<TValue>(object key) where TValue : class;

   void Save<TValue>(object id, TValue value) where TValue : class;
   void Delete<TEntity>(object id) where TEntity : class;
   void Save<TEntity>(TEntity entity) where TEntity : class, IEntity<Guid>;
   void Delete<TEntity>(TEntity entity) where TEntity : class, IEntity<Guid>;
}