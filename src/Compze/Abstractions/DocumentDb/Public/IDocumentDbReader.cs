using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Compze.Core.Public;

namespace Compze.Core.DocumentDb.Public;

public interface IDocumentDbReader : IDisposable
{
   TValue Get<TValue>(object key);
   bool TryGet<TValue>(object key, [MaybeNullWhen(false)]out TValue document);
   IEnumerable<T> GetAll<T>(IEnumerable<EntityId<Guid>> ids ) where T : IEntity<Guid>;
}