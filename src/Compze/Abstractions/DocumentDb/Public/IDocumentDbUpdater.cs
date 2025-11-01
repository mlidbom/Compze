using System;
using Compze.Core.Public;

namespace Compze.Core.DocumentDb.Public;

public interface IDocumentDbUpdater
{
   /// <summary>Like Get but, if supported by implementing class, eagerly locks the instance in the database.</summary>
   TValue GetForUpdate<TValue>(object key);

   void Save<TValue>(object id, TValue value);
   void Delete<TEntity>(object id);
   void Save<TEntity>(TEntity entity) where TEntity : IEntity<Guid>;
   void Delete<TEntity>(TEntity entity) where TEntity : IEntity<Guid>;
}