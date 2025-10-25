using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

public interface IEventStoreReader
{
   IReadOnlyList<IAggregateEvent> GetHistory(Guid aggregateId);
   /// <summary>
   /// Loads a specific version of the aggregate.
   /// This instance is NOT tracked for changes.
   /// No changes to this entity vill be persisted.
   /// </summary>
   TAggregate GetReadonlyCopyOfVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : class, IEventStored;

   TAggregate GetReadonlyCopy<TAggregate>(Guid aggregateId) where TAggregate : class, IEventStored;
}
