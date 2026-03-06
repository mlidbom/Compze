using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions.Public;

namespace Compze.DocumentDb.Public;

public interface IDocumentDbReader : IDisposable
{
   TValue Get<TValue>(object key) where TValue : class;
   bool TryGet<TValue>(object key, [NotNullWhen(true)]out TValue? document) where TValue : class;
   IEnumerable<T> GetAll<T>(IEnumerable<EntityId<Guid>> ids ) where T : IEntity<Guid>;
}