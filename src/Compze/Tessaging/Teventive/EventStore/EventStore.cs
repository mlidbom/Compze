using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Internal;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.PersistenceLayer.Abstractions;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.EventStore;

[UsedImplicitly] partial class EventStore : IEventStore
{
   readonly ITypeMapper _typeMapper;
   readonly IEventStoreSerializer _serializer;
   static readonly ILogger Log = CompzeLogger.For<EventStore>();

   readonly SingleThreadUseGuard _usageGuard;

   readonly IEventStorePersistenceLayer _persistenceLayer;

   readonly EventCache _cache;
   readonly IReadOnlyList<IEventMigration> _migrationFactories;

   internal static void RegisterWith(IDependencyRegistrar registrar, Func<IReadOnlyList<IEventMigration>> migrations)
      => registrar.Register(Scoped.For<IEventStore>()
                                  .CreatedBy((IEventStorePersistenceLayer persistenceLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache) =>
                                                new EventStore(persistenceLayer, typeMapper, serializer, cache, migrations())));

   public EventStore(IEventStorePersistenceLayer persistenceLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache, IEnumerable<IEventMigration> migrations)
   {
      _typeMapper = typeMapper;
      _serializer = serializer;
      Log.Debug("Constructor called");

      _migrationFactories = migrations.ToList();

      _usageGuard = new SingleThreadUseGuard();
      _cache = cache;
      _persistenceLayer = persistenceLayer;
   }

   public IReadOnlyList<IAggregateEvent> GetAggregateHistoryForUpdate(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: true);

   public IReadOnlyList<IAggregateEvent> GetAggregateHistory(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId, takeWriteLock: false);

   IReadOnlyList<IAggregateEvent> GetAggregateHistoryInternal(Guid aggregateId, bool takeWriteLock)
   {
      _usageGuard.AssertNoContextChangeOccurred(this);
      _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();

      var cachedAggregateHistory = _cache.Get(aggregateId);

      var newHistoryFromPersistenceLayer = GetAggregateEventsFromPersistenceLayer(aggregateId, takeWriteLock, cachedAggregateHistory.MaxSeenInsertedVersion);

      if(newHistoryFromPersistenceLayer.Length == 0)
      {
         return cachedAggregateHistory.Events;
      }

      var newerMigratedEventsExist = newHistoryFromPersistenceLayer.Where(IsRefactoringEvent).Any();

      var cachedMigratedHistoryExists = cachedAggregateHistory.MaxSeenInsertedVersion > 0;

      var migrationsHaveBeenPersistedWhileWeHeldEventsInCache = cachedMigratedHistoryExists && newerMigratedEventsExist;
      if(migrationsHaveBeenPersistedWhileWeHeldEventsInCache)
      {
         _cache.Remove(aggregateId);
         // ReSharper disable once TailRecursiveCall clarity over micro optimizations any day.
         return GetAggregateHistoryInternal(aggregateId, takeWriteLock);
      }

      var newEventsFromPersistenceLayer = newHistoryFromPersistenceLayer.Select(it => it.Event).ToArray();
      if(cachedAggregateHistory.Events.Count == 0)
      {
         AggregateHistoryValidator.ValidateHistory(aggregateId, newEventsFromPersistenceLayer);
      }

      var newAggregateHistory = cachedAggregateHistory.Events.Count == 0
                                   ? SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, newEventsFromPersistenceLayer)
                                   : cachedAggregateHistory.Events.Concat(newEventsFromPersistenceLayer)
                                                           .ToArray();

      if(cachedMigratedHistoryExists)
      {
         SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, newAggregateHistory);
      }

      var maxSeenInsertedVersion = newHistoryFromPersistenceLayer.Max(@event => @event.StorageInformation.InsertedVersion);
      AggregateHistoryValidator.ValidateHistory(aggregateId, newAggregateHistory);
      _cache.Store(aggregateId, new EventCache.Entry(events: newAggregateHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));

      return newAggregateHistory;
   }

   AggregateEvent HydrateEvent(EventDataRow eventDataRowRow)
   {
      var @event = (AggregateEvent)_serializer.Deserialize(eventType: _typeMapper.GetType(new TypeId(eventDataRowRow.EventType)), json: eventDataRowRow.EventJson);
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateEvent)@event).SetAggregateIdInternal(eventDataRowRow.AggregateId);
      ((IMutableAggregateEvent)@event).SetAggregateVersionInternal(eventDataRowRow.AggregateVersion);
      ((IMutableAggregateEvent)@event).SetMessageIdInternal(eventDataRowRow.EventId);
      ((IMutableAggregateEvent)@event).SetUtcTimeStampInternal(eventDataRowRow.UtcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
      return @event;
   }

   AggregateEventWithRefactoringInformation[] GetAggregateEventsFromPersistenceLayer(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
      => _persistenceLayer.GetAggregateHistory(aggregateId: aggregateId,
                                               startAfterInsertedVersion: startAfterInsertedVersion,
                                               takeWriteLock: takeWriteLock)
                          .Select(it => new AggregateEventWithRefactoringInformation(HydrateEvent(it), it.StorageInformation))
                          .ToArray();

   static bool IsRefactoringEvent(AggregateEventWithRefactoringInformation @event) => @event.StorageInformation.RefactoringInformation != null;

   IEnumerable<IAggregateEvent> StreamEvents(int batchSize)
   {
      var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
      return streamMutator.Mutate(_persistenceLayer.StreamEvents(batchSize).Select(HydrateEvent));
   }

   public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateEvent>> handleEvents)
   {
      _usageGuard.AssertNoContextChangeOccurred(this);
      _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();

      var batches = StreamEvents(batchSize)
                   .ChopIntoSizesOf(batchSize)
                   .Select(batch => batch.ToList());
      foreach(var batch in batches)
      {
         handleEvents(batch);
      }
   }

   public void SaveSingleAggregateEvents(IReadOnlyList<IAggregateEvent> aggregateEvents)
   {
      _usageGuard.AssertNoContextChangeOccurred(this);
      _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();

      var aggregateId = aggregateEvents[0].AggregateId;

      if(aggregateEvents.Any(it => it.AggregateId != aggregateId))
      {
         throw new ArgumentException("Got events from multiple Aggregates. This is not supported.");
      }

      var cacheEntry = _cache.Get(aggregateId);
      var specifications = aggregateEvents.Select(@event => cacheEntry.CreateInsertionSpecificationForNewEvent(@event)).ToArray();

      var eventRows = aggregateEvents
                     .Select(@event => new EventDataRow(specification: cacheEntry.CreateInsertionSpecificationForNewEvent(@event), _typeMapper.GetId(@event.GetType()).GuidValue, eventAsJson: _serializer.Serialize((AggregateEvent)@event)))
                     .ToList();

      eventRows.ForEach(it => it.StorageInformation.EffectiveVersion = it.AggregateVersion);
      _persistenceLayer.InsertSingleAggregateEvents(eventRows);

      var completeAggregateHistory = cacheEntry
                                    .Events.Concat(aggregateEvents)
                                    .Cast<AggregateEvent>()
                                    .ToArray();
      SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeAggregateHistory);
      AggregateHistoryValidator.ValidateHistory(aggregateId, completeAggregateHistory);

      _cache.Store(aggregateId,
                   new EventCache.Entry(completeAggregateHistory,
                                        maxSeenInsertedVersion: specifications.Max(specification => specification.InsertedVersion)));
   }

   public void DeleteAggregate(Guid aggregateId)
   {
      _usageGuard.AssertNoContextChangeOccurred(this);
      _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();
      _cache.Remove(aggregateId);
      _persistenceLayer.DeleteAggregate(aggregateId);
   }

   public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null)
   {
      Assert.Argument.Is(eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventBaseType));
      _usageGuard.AssertNoContextChangeOccurred(this);

      _persistenceLayer.SetupSchemaIfDatabaseUnInitialized();
      return _persistenceLayer.ListAggregateIdsInCreationOrder()
                              .Where(it => eventBaseType == null || eventBaseType.IsAssignableFrom(_typeMapper.GetType(new TypeId(it.TypeId))))
                              .Select(it => it.AggregateId);
   }

   class AggregateEventWithRefactoringInformation(AggregateEvent @event, AggregateEventStorageInformation storageInformation)
   {
      internal AggregateEvent Event { get; } = @event;
      internal AggregateEventStorageInformation StorageInformation { get; } = storageInformation;
   }

   public void Dispose() {}
}
