using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Compze.Core.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.QueryModels.Generators.Public;

public interface IQueryModelReader
{
   //todo review the object usage here
   TValue Get<TValue>(EntityId key);
   bool TryGet<TValue>(EntityId key, [MaybeNullWhen(false)] out TValue document);
   IEnumerable<T> GetAll<T>(IEnumerable<EntityId> ids) where T : IEntity;
}

public interface IVersioningQueryModelReader : IQueryModelReader
{
   bool TryGetVersion<TDocument>(EntityId key, [MaybeNullWhen(false)] out TDocument document, int version);
   TValue GetVersion<TValue>(EntityId key, int version);
}
