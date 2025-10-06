using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;

abstract class CompleteEventStoreStreamMutator
{
   public static ICompleteEventStreamMutator Create(IReadOnlyList<IEventMigration> eventMigrationFactories) => eventMigrationFactories.Any()
                                                                                                                  ? new RealMutator(eventMigrationFactories)
                                                                                                                  : new OnlySerializeVersionsMutator();

   class OnlySerializeVersionsMutator : ICompleteEventStreamMutator
   {
      readonly Dictionary<Guid, int> _aggregateVersions = new();

      public IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream)
      {
         foreach(var @event in eventStream)
         {
            var version = _aggregateVersions.GetOrAddDefault(@event.AggregateId) + 1;
            _aggregateVersions[@event.AggregateId] = version;
#pragma warning disable CS0618 // Type or member is obsolete
            ((IMutableAggregateEvent)@event).SetAggregateVersionInternal(version);
#pragma warning restore CS0618 // Type or member is obsolete
            yield return @event;
         }
      }
   }

   class RealMutator(IReadOnlyList<IEventMigration> eventMigrationFactories) : ICompleteEventStreamMutator
   {
      readonly IReadOnlyList<IEventMigration> _eventMigrationFactories = eventMigrationFactories;
      readonly Dictionary<Guid, ISingleAggregateInstanceEventStreamMutator> _aggregateMutatorsCache = new();

      public IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream)
      {
         foreach(var @event in eventStream)
         {
            var mutatedEvents = _aggregateMutatorsCache.GetOrAdd(
               @event.AggregateId,
               () => SingleAggregateInstanceEventStreamMutator.Create(@event, _eventMigrationFactories)
            ).Mutate(@event);

            foreach(var mutatedEvent in mutatedEvents)
            {
               yield return mutatedEvent;
            }
         }

         // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
         foreach (var mutator in _aggregateMutatorsCache)
         {
            foreach (var finalEvent in mutator.Value.EndOfAggregate())
            {
               yield return finalEvent;
            }
         }
      }
   }
}