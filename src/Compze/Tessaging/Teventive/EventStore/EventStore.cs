using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time;
using Compze.Sql.Common.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.EventStore;

[UsedImplicitly] partial class EventStore : IEventStore
{
   readonly ITypeMapper _typeMapper;
   readonly IEventStoreSerializer _serializer;
   static readonly ILogger Log = CompzeLogger.For<EventStore>();

   readonly SingleThreadUseGuard _usageGuard;

   readonly IEventStoreSqlLayer _sqlLayer;

   readonly EventCache _cache;
   readonly IReadOnlyList<IEventMigration> _migrationFactories;

   internal static void RegisterWith(IComponentRegistrar registrar, Func<IReadOnlyList<IEventMigration>> migrations)
      => registrar.Register(Scoped.For<IEventStore>()
                                  .CreatedBy((IEventStoreSqlLayer sqlLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache) =>
                                                new EventStore(sqlLayer, typeMapper, serializer, cache, migrations())));

   public EventStore(IEventStoreSqlLayer sqlLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache, IEnumerable<IEventMigration> migrations)
   {
      _typeMapper = typeMapper;
      _serializer = serializer;
      Log.Debug("Constructor called");

      _migrationFactories = migrations.ToList();

      _usageGuard = new SingleThreadUseGuard(this);
      _cache = cache;
      _sqlLayer = sqlLayer;
   }

   public IReadOnlyList<IAggregateTevent> GetAggregateHistoryForUpdate(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: true);

   public IReadOnlyList<IAggregateTevent> GetAggregateHistory(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId, takeWriteLock: false);

   IReadOnlyList<IAggregateTevent> GetAggregateHistoryInternal(Guid aggregateId, bool takeWriteLock)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var cachedAggregateHistory = _cache.Get(aggregateId);

      var newHistoryFromSqlLayer = GetAggregateEventsFromSqlLayer(aggregateId, takeWriteLock, cachedAggregateHistory.MaxSeenInsertedVersion);

      if(newHistoryFromSqlLayer.Length == 0)
      {
         return cachedAggregateHistory.Events;
      }

      var newerMigratedEventsExist = newHistoryFromSqlLayer.Where(IsRefactoringEvent).Any();

      var cachedMigratedHistoryExists = cachedAggregateHistory.MaxSeenInsertedVersion > 0;

      var migrationsHaveBeenPersistedWhileWeHeldEventsInCache = cachedMigratedHistoryExists && newerMigratedEventsExist;
      if(migrationsHaveBeenPersistedWhileWeHeldEventsInCache)
      {
         _cache.Remove(aggregateId);
         // ReSharper disable once TailRecursiveCall clarity over micro optimizations any day.
         return GetAggregateHistoryInternal(aggregateId, takeWriteLock);
      }

      var newEventsFromSqlLayer = newHistoryFromSqlLayer.Select(it => it.Tevent).ToArray();
      if(cachedAggregateHistory.Events.Count == 0)
      {
         AggregateHistoryValidator.ValidateHistory(aggregateId, newEventsFromSqlLayer);
      }

      var newAggregateHistory = cachedAggregateHistory.Events.Count == 0
                                   ? SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, newEventsFromSqlLayer)
                                   : cachedAggregateHistory.Events.Concat(newEventsFromSqlLayer)
                                                           .ToArray();

      if(cachedMigratedHistoryExists)
      {
         SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, newAggregateHistory);
      }

      var maxSeenInsertedVersion = newHistoryFromSqlLayer.Max(@event => @event.StorageInformation.InsertedVersion);
      AggregateHistoryValidator.ValidateHistory(aggregateId, newAggregateHistory);
      _cache.Store(aggregateId, new EventCache.Entry(events: newAggregateHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));

      return newAggregateHistory;
   }

   AggregateTevent HydrateEvent(EventDataRow eventDataRowRow)
   {
      var @event = (AggregateTevent)_serializer.Deserialize(eventType: _typeMapper.GetType(new TypeId(eventDataRowRow.EventType)), json: eventDataRowRow.EventJson);
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateTevent)@event).SetAggregateIdInternal(eventDataRowRow.AggregateId);
      ((IMutableAggregateTevent)@event).SetAggregateVersionInternal(eventDataRowRow.AggregateVersion);
      ((IMutableAggregateTevent)@event).SetMessageIdInternal(eventDataRowRow.EventId);
      ((IMutableAggregateTevent)@event).SetUtcTimeStampInternal(eventDataRowRow.UtcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
      return @event;
   }

   AggregateEventWithRefactoringInformation[] GetAggregateEventsFromSqlLayer(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
      => _sqlLayer.GetAggregateHistory(aggregateId: aggregateId,
                                               startAfterInsertedVersion: startAfterInsertedVersion,
                                               takeWriteLock: takeWriteLock)
                          .Select(it => new AggregateEventWithRefactoringInformation(HydrateEvent(it), it.StorageInformation))
                          .ToArray();

   static bool IsRefactoringEvent(AggregateEventWithRefactoringInformation @event) => @event.StorageInformation.RefactoringInformation != null;

   IEnumerable<IAggregateTevent> StreamEvents(int batchSize)
   {
      var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
      return streamMutator.Mutate(_sqlLayer.StreamEvents(batchSize).Select(HydrateEvent));
   }

   public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateTevent>> handleEvents)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var batches = StreamEvents(batchSize)
                   .ChopIntoSizesOf(batchSize)
                   .Select(batch => batch.ToList());
      foreach(var batch in batches)
      {
         handleEvents(batch);
      }
   }

   public void SaveSingleAggregateEvents(IReadOnlyList<IAggregateTevent> aggregateEvents)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var aggregateId = aggregateEvents[0].AggregateId;

      if(aggregateEvents.Any(it => it.AggregateId != aggregateId))
      {
         throw new ArgumentException("Got events from multiple Aggregates. This is not supported.");
      }

      var cacheEntry = _cache.Get(aggregateId);
      var specifications = aggregateEvents.Select(@event => cacheEntry.CreateInsertionSpecificationForNewEvent(@event)).ToArray();

      var eventRows = aggregateEvents
                     .Select(@event => new EventDataRow(specification: cacheEntry.CreateInsertionSpecificationForNewEvent(@event), _typeMapper.GetId(@event.GetType()).GuidValue, eventAsJson: _serializer.Serialize((AggregateTevent)@event)))
                     .ToList();

      eventRows.ForEach(it => it.StorageInformation.EffectiveVersion = it.AggregateVersion);
      _sqlLayer.InsertSingleAggregateEvents(eventRows);

      var completeAggregateHistory = cacheEntry
                                    .Events.Concat(aggregateEvents)
                                    .Cast<AggregateTevent>()
                                    .ToArray();
      SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeAggregateHistory);
      AggregateHistoryValidator.ValidateHistory(aggregateId, completeAggregateHistory);

      _cache.Store(aggregateId,
                   new EventCache.Entry(completeAggregateHistory,
                                        maxSeenInsertedVersion: specifications.Max(specification => specification.InsertedVersion)));
   }

   public void DeleteAggregate(Guid aggregateId)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();
      _cache.Remove(aggregateId);
      _sqlLayer.DeleteAggregate(aggregateId);
   }

   public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventBaseType = null)
   {
      Assert.Argument.Is(eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateTevent).IsAssignableFrom(eventBaseType));
      _usageGuard.EnsureAccessValid();

      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();
      return _sqlLayer.ListAggregateIdsInCreationOrder()
                              .Where(it => eventBaseType == null || eventBaseType.IsAssignableFrom(_typeMapper.GetType(new TypeId(it.TypeId))))
                              .Select(it => it.AggregateId);
   }

   class AggregateEventWithRefactoringInformation(AggregateTevent tevent, AggregateEventStorageInformation storageInformation)
   {
      internal AggregateTevent Tevent { get; } = tevent;
      internal AggregateEventStorageInformation StorageInformation { get; } = storageInformation;
   }

   public void Dispose() {}
}
