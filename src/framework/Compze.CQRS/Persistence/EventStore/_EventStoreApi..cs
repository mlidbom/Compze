using System;

// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent APIs. No statics please.

namespace Compze.Persistence.EventStore;

public partial class EventStoreApi
{
   public QueryApi Queries => new();
   public Command Commands => new();

   public partial class QueryApi
   {
      public AggregateLink<TAggregate> GetForUpdate<TAggregate>(Guid id) where TAggregate : class, IEventStored => new(id);

      public GetReadonlyCopyOfAggregate<TAggregate> GetReadOnlyCopy<TAggregate>(Guid id) where TAggregate : class, IEventStored => new(id);

      public GetReadonlyCopyOfAggregateVersion<TAggregate> GetReadOnlyCopyOfVersion<TAggregate>(Guid id, int version) where TAggregate : class, IEventStored => new(id, version);

      public GetAggregateHistory<TEvent> GetHistory<TEvent>(Guid id) where TEvent : IAggregateEvent => new(id);
   }

   public partial class Command
   {
      public SaveAggregate<TAggregate> Save<TAggregate>(TAggregate account) where TAggregate : class, IEventStored => new(account);
   }
}