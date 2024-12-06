using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Compze.Contracts;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.Persistence.EventStore.Refactoring.Migrations;
using Compze.SystemCE;
using Compze.SystemCE.LinqCE;
using ReadOrder = Compze.Persistence.EventStore.PersistenceLayer.ReadOrder;

namespace Compze.Persistence.EventStore;

partial class EventStore
{
   public void PersistMigrations()
   {
      Assert.State.Is(Transaction.Current == null, () => $"Cannot run {nameof(PersistMigrations)} within a transaction. Internally manages transactions.");
      Log.Warning("Starting to persist migrations");

      long migratedAggregates = 0;
      long updatedAggregates = 0;
      long newEventCount = 0;
      var logInterval = 1.Minutes();
      var lastLogTime = DateTime.Now;

      const int recoverableErrorRetriesToMake = 5;
      var exceptions = new List<(Guid AggregateId,Exception Exception)>();

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

                  var highestSeenVersion = original.Max(@event => @event.StorageInformation.InsertedVersion) + 1;

                  var updatedAggregatesBeforeMigrationOfThisAggregate = updatedAggregates;

                  var refactorings = new List<List<EventDataRow>>();

                  var inMemoryMigratedHistory = SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
                     _migrationFactories,
                     original.Select(it => it.Event).ToArray(),
                     newEvents =>
                     {
                        //Make sure we don't try to insert into an occupied InsertedVersion
                        newEvents.ForEach(refactoredEvent =>
                        {
                           refactoredEvent.StorageInformation.InsertedVersion = highestSeenVersion++;
                        });

                        refactorings.Add(newEvents
                                        .Select(it => new EventDataRow(@event: it.NewEvent,
                                                                          it.StorageInformation,
                                                                          _typeMapper.GetId(it.NewEvent.GetType()).GuidValue,
                                                                          eventAsJson: _serializer.Serialize(it.NewEvent)))
                                        .ToList());

                        updatedAggregates = updatedAggregatesBeforeMigrationOfThisAggregate + 1;
                        newEventCount += newEvents.Count;
                     });

                  if(refactorings.Count > 0)
                  {
                     refactorings.ForEach(InsertEventsForSingleRefactoring);

                     FixManualVersions(original, inMemoryMigratedHistory, refactorings);

                     var loadedAggregateHistory = GetAggregateHistoryInternal(aggregateId, takeWriteLock:false);
                     AggregateHistoryValidator.ValidateHistory(aggregateId, loadedAggregateHistory);
                     AssertHistoriesAreIdentical(inMemoryMigratedHistory, loadedAggregateHistory);
                  }

                  migratedAggregates++;
                  succeeded = true;
                  transaction.Complete();
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
            exceptions.Add((aggregateId, exception));
         }

         if(logInterval < DateTime.Now - lastLogTime)
         {
            lastLogTime = DateTime.Now;
            // ReSharper disable once AccessToModifiedClosure
            int PercentDone() => (int)(double)migratedAggregates / aggregateIdsInCreationOrder.Count * 100;

            Log.Info($"{PercentDone()}% done. Inspected: {migratedAggregates} / {aggregateIdsInCreationOrder.Count}, Updated: {updatedAggregates}, New Events: {newEventCount}");
         }
      }

      Log.Warning("Done persisting migrations.");
      Log.Info($"Inspected: {migratedAggregates} , Updated: {updatedAggregates}, New Events: {newEventCount}");
      if(exceptions.Any())
      {
         throw new AggregateException($"""

                                       Failed to persist {exceptions.Count} migrations. 

                                       AggregateIds: 
                                       {exceptions.Select(it => it.AggregateId.ToString()).Join($",{Environment.NewLine}")}
                                       """, exceptions.Select(it => it.Exception));
      }

   }

   void FixManualVersions(AggregateEventWithRefactoringInformation[] originalHistory, AggregateEvent[] newHistory, IReadOnlyList<List<EventDataRow>> refactorings)
   {
      var versionUpdates = new List<VersionSpecification>();
      var replacedOrRemoved = originalHistory.Where(it => newHistory.None(@event => @event.MessageId == it.Event.MessageId)).ToList();
      versionUpdates.AddRange(replacedOrRemoved.Select(it => new VersionSpecification(it.Event.MessageId, -it.StorageInformation.EffectiveVersion)));

      var replacedOrRemoved2 = refactorings.SelectMany(it =>it).Where(it => newHistory.None(@event => @event.MessageId == it.EventId));
      versionUpdates.AddRange(replacedOrRemoved2.Select(it => new VersionSpecification(it.EventId, -it.StorageInformation.EffectiveVersion)));

      //Performance: Filter out rows where the new value equals the old value. We don't want to go updating every event in every refactored aggregate if only a few, or none, have actually changed.
      versionUpdates.AddRange(newHistory.Select((it , index) => new VersionSpecification(it.MessageId, index + 1)));

      _persistenceLayer.UpdateEffectiveVersions(versionUpdates);
   }

   void AssertHistoriesAreIdentical(AggregateEvent[] inMemoryMigratedHistory, IReadOnlyList<IAggregateEvent> loadedAggregateHistory)
   {
      Assert.Result.Is(inMemoryMigratedHistory.Length == loadedAggregateHistory.Count);
      for(var index = 0; index < inMemoryMigratedHistory.Length; ++index)
      {
         var inMemory = inMemoryMigratedHistory[index];
         var loaded = loadedAggregateHistory[index];
         Assert.Result
               .Is(inMemory.AggregateId == loaded.AggregateId)
               .Is(inMemory.MessageId == loaded.MessageId)
               .Is(inMemory.AggregateVersion == loaded.AggregateVersion)
               .Is(inMemory.UtcTimeStamp == loaded.UtcTimeStamp)
               .Is(inMemory.GetType() == loaded.GetType())
               .Is(_serializer.Serialize(inMemory) == _serializer.Serialize((AggregateEvent)loaded));
      }
   }

   void InsertEventsForSingleRefactoring(IReadOnlyList<EventDataRow> events)
   {
      var refactoring = events[0].StorageInformation.RefactoringInformation!;

      switch(refactoring.RefactoringType)
      {
         case AggregateEventRefactoringType.Replace:
            ReplaceEvent(refactoring.TargetEvent, events.ToArray());
            break;
         case AggregateEventRefactoringType.InsertBefore:
            InsertBeforeEvent(refactoring.TargetEvent, events.ToArray());
            break;
         case AggregateEventRefactoringType.InsertAfter:
            InsertAfterEvent(refactoring.TargetEvent, events.ToArray());
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   void InsertAfterEvent(Guid eventId, EventDataRow[] insertAfterGroup)
   {
      var eventToInsertAfter = _persistenceLayer.LoadEventNeighborHood(eventId);

      SetManualReadOrders(newEvents: insertAfterGroup,
                          rangeStart: eventToInsertAfter.EffectiveReadOrder,
                          rangeEnd: eventToInsertAfter.NextEventReadOrder);

      _persistenceLayer.InsertSingleAggregateEvents(insertAfterGroup);
   }

   void InsertBeforeEvent(Guid eventId, EventDataRow[] insertBefore)
   {
      var eventToInsertBefore = _persistenceLayer.LoadEventNeighborHood(eventId);

      SetManualReadOrders(newEvents: insertBefore,
                          rangeStart: eventToInsertBefore.PreviousEventReadOrder,
                          rangeEnd: eventToInsertBefore.EffectiveReadOrder);

      _persistenceLayer.InsertSingleAggregateEvents(insertBefore);
   }

   void ReplaceEvent(Guid eventId, EventDataRow[] replacementEvents)
   {
      var neighborHood = _persistenceLayer.LoadEventNeighborHood(eventId);

      //We are not making maximally efficient use of the space here. Since the replaced event is no longer in use we should theoretically be able to start the range at the previous events position.
      //To make this possible without a collision on the unique index the replaced events read order would need to be moved out of the way somehow. Negating it seems easy but actually introduces risk of collisions.
      //Replacing an event that had previously replaced another event would be likely to result in trying to save the same negative (removed) read order again.
      //Fixing this seems rather non-trivial, so for now we keep the read orders of replaced events in place and accept that we do not use the space optimally.
      //Removing the unique constraint would work, but would make us more vulnerable to data corruption issues.
      SetManualReadOrders(newEvents: replacementEvents,
                          rangeStart: neighborHood.EffectiveReadOrder,
                          rangeEnd: neighborHood.NextEventReadOrder);

      _persistenceLayer.InsertSingleAggregateEvents(replacementEvents);
   }

   static void SetManualReadOrders(EventDataRow[] newEvents, ReadOrder rangeStart, ReadOrder rangeEnd)
   {
      var readOrders = ReadOrder.CreateOrdersForEventsBetween(newEvents.Length, rangeStart, rangeEnd);
      for (var index = 0; index < newEvents.Length; index++)
      {
         newEvents[index].StorageInformation.ReadOrder = readOrders[index];
      }
   }

   static bool IsRecoverableSqlException(Exception exception)
   {
      var message = exception.Message.ToUpperInvariant();
      return message.ContainsInvariant("TIMEOUT") || message.ContainsInvariant("DEADLOCK");
   }
}