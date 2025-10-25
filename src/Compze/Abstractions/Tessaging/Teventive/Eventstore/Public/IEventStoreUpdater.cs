using System;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

public interface IEventStoreUpdater : IDisposable
{
   /// <summary>
   /// Loads an aggregate and tracks it for changes.
   /// </summary>
   TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : class, IEventStored;

   /// <summary>
   /// Causes the store to start tracking the aggregate.
   /// </summary>
   void Save<TAggregate>(TAggregate aggregate) where TAggregate : class, IEventStored;

   /// <summary>
   /// Tries to get the specified instance. Returns false and sets the result to null if the aggregate did not exist.
   /// </summary>
   bool TryGet<TAggregate>(Guid aggregateId, out TAggregate? result) where TAggregate : class, IEventStored;

   /// <summary>
   /// Deletes all traces of an aggregate from the store.
   /// </summary>
   void Delete(Guid aggregateId);
}