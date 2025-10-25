using System;
using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;

public interface ITeventStoreReader
{
   IReadOnlyList<IAggregateTevent> GetHistory(Guid aggregateId);
   /// <summary>
   /// Loads a specific version of the aggregate.
   /// This instance is NOT tracked for changes.
   /// No changes to this entity vill be persisted.
   /// </summary>
   TAggregate GetReadonlyCopyOfVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : class, ITeventStored;

   TAggregate GetReadonlyCopy<TAggregate>(Guid aggregateId) where TAggregate : class, ITeventStored;
}
