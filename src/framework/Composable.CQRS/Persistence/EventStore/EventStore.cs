using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Composable.Contracts;
using Composable.Logging;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.Persistence.EventStore
{
    class EventStore : IEventStore
    {
        readonly ITypeMapper _typeMapper;
        readonly IEventStoreSerializer _serializer;
        static readonly ILogger Log = Logger.For<EventStore>();

        readonly ISingleContextUseGuard _usageGuard;

        readonly IEventStoreEventReader _eventReader;
        readonly IEventStoreEventWriter _eventWriter;
        readonly EventCache _cache;
        readonly IEventStoreSchemaManager _schemaManager;
        readonly IReadOnlyList<IEventMigration> _migrationFactories;

        public EventStore(IEventStorePersistenceLayer persistenceLayer, ITypeMapper typeMapper, IEventStoreSerializer serializer, EventCache cache, IEnumerable<IEventMigration> migrations)
        {
            _typeMapper = typeMapper;
            _serializer = serializer;
            Log.Debug("Constructor called");

            _migrationFactories = migrations.ToList();

            _usageGuard = new SingleThreadUseGuard();
            _cache = cache;
            _schemaManager = persistenceLayer.SchemaManager;
            _eventReader = persistenceLayer.EventReader;
            _eventWriter = persistenceLayer.EventWriter;
        }

        public IReadOnlyList<IAggregateEvent> GetAggregateHistoryForUpdate(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: true);

        public IReadOnlyList<IAggregateEvent> GetAggregateHistory(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId, takeWriteLock: false);

        IReadOnlyList<IAggregateEvent> GetAggregateHistoryInternal(Guid aggregateId, bool takeWriteLock)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

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

            var newEventsFromPersistenceLayer = newHistoryFromPersistenceLayer.Select(@this => @this.Event).ToArray();
            var newAggregateHistory = cachedAggregateHistory.Events.Count == 0
                                        ? SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, newEventsFromPersistenceLayer)
                                        : cachedAggregateHistory.Events.Concat(newEventsFromPersistenceLayer)
                                                                .ToArray();


            if(cachedMigratedHistoryExists)
            {
                SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, newAggregateHistory);
            }

            var maxSeenInsertedVersion =  newHistoryFromPersistenceLayer.Max(@event => @event.RefactoringInformation.InsertedVersion);
            _cache.Store(aggregateId, new EventCache.Entry(events: newAggregateHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));

            return newAggregateHistory;
        }

        AggregateEvent HydrateEvent(EventDataRow eventDataRowRow)
        {
            var @event = (AggregateEvent)_serializer.Deserialize(eventType: _typeMapper.GetType(eventDataRowRow.EventType), json: eventDataRowRow.EventJson);
            @event.AggregateId = eventDataRowRow.AggregateId;
            @event.AggregateVersion = eventDataRowRow.AggregateVersion;
            @event.EventId = eventDataRowRow.EventId;
            @event.UtcTimeStamp = eventDataRowRow.UtcTimeStamp;
            @event.StorageInformation.RefactoringInformation = eventDataRowRow.RefactoringInformation;
            return @event;
        }

        AggregateEventWithRefactoringInformation[] GetAggregateEventsFromPersistenceLayer(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
            => _eventReader.GetAggregateHistory(aggregateId: aggregateId,
                                                startAfterInsertedVersion: startAfterInsertedVersion,
                                                takeWriteLock: takeWriteLock)
                           .Select(@this => new AggregateEventWithRefactoringInformation(HydrateEvent(@this), @this.RefactoringInformation) )
                           .ToArray();

        static bool IsRefactoringEvent(AggregateEventWithRefactoringInformation @event) => @event.RefactoringInformation.InsertBefore.HasValue || @event.RefactoringInformation.InsertAfter.HasValue || @event.RefactoringInformation.Replaces.HasValue;

        IEnumerable<IAggregateEvent> StreamEvents(int batchSize)
        {
            var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
            return streamMutator.Mutate(_eventReader.StreamEvents(batchSize).Select(HydrateEvent));
        }

        public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateEvent>> handleEvents)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            var batches = StreamEvents(batchSize)
                .ChopIntoSizesOf(batchSize)
                .Select(batch => batch.ToList());
            foreach (var batch in batches)
            {
                handleEvents(batch);
            }
        }

        public void SaveSingleAggregateEvents(IReadOnlyList<IAggregateEvent> aggregateEvents)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            var aggregateId = aggregateEvents.First().AggregateId;

            if(aggregateEvents.Any(@this => @this.AggregateId != aggregateId))
            {
                throw new ArgumentException("Got events from multiple Aggregates. This is not supported.");
            }

            var cacheEntry = _cache.Get(aggregateId);
            var specifications = aggregateEvents.Select(@event => cacheEntry.CreateInsertionSpecificationForNewEvent(@event)).ToArray();

            var eventRows = aggregateEvents
                           .Select(@event => new EventDataRow(specification: cacheEntry.CreateInsertionSpecificationForNewEvent(@event), _typeMapper.GetId(@event.GetType()), eventAsJson: _serializer.Serialize((AggregateEvent)@event)))
                           .ToList();
            _eventWriter.Insert(eventRows);

            var completeAggregateHistory = cacheEntry
                                          .Events.Concat(aggregateEvents)
                                          .Cast<AggregateEvent>()
                                          .ToArray();
            SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeAggregateHistory);

            _cache.Store(aggregateId, new EventCache.Entry(completeAggregateHistory,
                                                           maxSeenInsertedVersion: specifications.Max(specification => specification.InsertedVersion)));
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            _cache.Remove(aggregateId);
            _eventWriter.DeleteAggregate(aggregateId);
        }



        public void PersistMigrations()
        {
            Log.Warning("Starting to persist migrations");

            long migratedAggregates = 0;
            long updatedAggregates = 0;
            long newEventCount = 0;
            var logInterval = 1.Minutes();
            var lastLogTime = DateTime.Now;

            const int recoverableErrorRetriesToMake = 5;

            var aggregateIdsInCreationOrder = StreamAggregateIdsInCreationOrder().ToList();

            foreach (var aggregateId in aggregateIdsInCreationOrder)
            {
                try
                {
                    var succeeded = false;
                    var retries = 0;
                    while(!succeeded)
                    {
                        try
                        {
                            //performance: bug: Look at ways to avoid taking a lock for a long time as we do now. This might be a problem in production.
                            using var transaction = new TransactionScope(TransactionScopeOption.Required, scopeTimeout: 10.Minutes());
                            var original = GetAggregateEventsFromPersistenceLayer(aggregateId: aggregateId, takeWriteLock: true);

                            var startInsertingWithVersion = original.Max(@event => @event.RefactoringInformation.InsertedVersion) + 1;

                            var updatedAggregatesBeforeMigrationOfThisAggregate = updatedAggregates;

                            SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
                                _migrationFactories,
                                original.Select(@this => @this.Event).ToArray(),
                                newEvents =>
                                {
                                    //Make sure we don't try to insert into an occupied InsertedVersion
                                    newEvents.ForEach(refactoredEvent =>
                                    {
                                        refactoredEvent.RefactoringInformation.InsertedVersion = startInsertingWithVersion++;
                                    });
                                    //Save all new events so they get an InsertionOrder for the next refactoring to work with in case it acts relative to any of these events
                                    var eventRows = newEvents
                                                   .Select(@this => new EventDataRow(@event: @this.NewEvent, @this.RefactoringInformation, _typeMapper.GetId(@this.NewEvent.GetType()), eventAsJson: _serializer.Serialize(@this.NewEvent)))
                                                   .ToList();

                                    _eventWriter.InsertRefactoringEvents(eventRows);

                                    updatedAggregates = updatedAggregatesBeforeMigrationOfThisAggregate + 1;
                                    newEventCount += newEvents.Count;
                                });

                            transaction.Complete();

                            migratedAggregates++;
                            succeeded = true;
                        }
                        catch(Exception e) when(IsRecoverableSqlException(e) && ++retries <= recoverableErrorRetriesToMake)
                        {
                            Log.Warning(e, $"Failed to persist migrations for aggregate: {aggregateId}. Exception appears to be recoverable so running retry {retries} out of {recoverableErrorRetriesToMake}");
                        }
                    }
                }
                catch(Exception exception)
                {
                    Log.Error(exception, $"Failed to persist migrations for aggregate: {aggregateId}");
                }

                if(logInterval < DateTime.Now - lastLogTime)
                {
                    lastLogTime = DateTime.Now;
                    // ReSharper disable once AccessToModifiedClosure
                    int PercentDone() => (int)(((double)migratedAggregates / aggregateIdsInCreationOrder.Count) * 100);

                    Log.Info($"{PercentDone()}% done. Inspected: {migratedAggregates} / {aggregateIdsInCreationOrder.Count}, Updated: {updatedAggregates}, New Events: {newEventCount}");
                }
            }

            Log.Warning("Done persisting migrations.");
            Log.Info($"Inspected: {migratedAggregates} , Updated: {updatedAggregates}, New Events: {newEventCount}");

        }

        static bool IsRecoverableSqlException(Exception exception)
        {
            var message = exception.Message.ToLower();
            return message.Contains("timeout") || message.Contains("deadlock");
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? eventType = null)
        {
            Contract.Assert.That(eventType == null || eventType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventType),
                "eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateEvent).IsAssignableFrom(eventType)");
            _usageGuard.AssertNoContextChangeOccurred(this);

            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            return _eventReader.StreamAggregateIdsInCreationOrder(eventType);
        }

        public void Dispose()
        {
        }

        class AggregateEventWithRefactoringInformation
        {
            public AggregateEventWithRefactoringInformation(AggregateEvent @event, AggregateEventRefactoringInformation refactoringInformation)
            {
                Event = @event;
                RefactoringInformation = refactoringInformation;
            }

            internal AggregateEvent Event { get; }
            internal AggregateEventRefactoringInformation RefactoringInformation { get; }
        }
    }
}