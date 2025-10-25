using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.Public;

public interface ITeventStoreReader
{
   IReadOnlyList<ITaggregateTevent> GetHistory(Guid taggregateId);
   /// <summary>
   /// Loads a specific version of the taggregate.
   /// This instance is NOT tracked for changes.
   /// No changes to this entity vill be persisted.
   /// </summary>
   TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(Guid taggregateId, int version) where TTaggregate : class, ITeventStored;

   TTaggregate GetReadonlyCopy<TTaggregate>(Guid taggregateId) where TTaggregate : class, ITeventStored;
}
