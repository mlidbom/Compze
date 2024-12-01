using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using Compze.SystemCE.TransactionsCE;
using ReadOrder = Compze.Persistence.EventStore.PersistenceLayer.ReadOrder;

namespace Compze.Persistence.InMemory.EventStore;

partial class InMemoryEventStorePersistenceLayer : IEventStorePersistenceLayer
{
   readonly IThreadShared<State> _state = ThreadShared.WithDefaultTimeout(new State());
   readonly TransactionLockManager _transactionLockManager = new();

   public InMemoryEventStorePersistenceLayer() => _state.Update(state => state.Init(_state));

   public void InsertSingleAggregateEvents(IReadOnlyList<EventDataRow> events) =>
      _transactionLockManager.WithTransactionWideLock(
         events[0].AggregateId,
         () => _state.Update(state =>
         {
            events.ForEach((@event, index) => @event.StorageInformation.ReadOrder ??= ReadOrder.FromLong(state.Events.Count + index + 1));
            state.AddRange(events);
         }));

   public IReadOnlyList<EventDataRow> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
      => _transactionLockManager.WithTransactionWideLock(
         aggregateId,
         takeWriteLock,
         () => _state.Update(
            state => state
                    .Events
                    .OrderBy(it => it.StorageInformation.ReadOrder)
                    .Where(it => it.AggregateId == aggregateId
                                 && it.StorageInformation.InsertedVersion > startAfterInsertedVersion
                                 && it.StorageInformation.EffectiveVersion > 0)
                    .ToArray()));

   public void UpdateEffectiveVersions(IReadOnlyList<VersionSpecification> versions)
      => _transactionLockManager.WithTransactionWideLock(
         _state.Update(state => state.Events.Single(@event => @event.EventId == versions[0].EventId)).AggregateId,
         () => _state.Update(
            state =>
            {
               foreach(var specification in versions)
               {
                  var (@event, index) = state.Events
                                             .Select((eventRow, innerIndex) => (eventRow, innerIndex))
                                             .Single(it => it.eventRow.EventId == specification.EventId);

                  state.ReplaceEvent(index,
                                     new EventDataRow(@event.EventType,
                                                      @event.EventJson,
                                                      @event.EventId,
                                                      specification.EffectiveVersion,
                                                      @event.AggregateId,
                                                      @event.UtcTimeStamp,
                                                      new AggregateEventStorageInformation
                                                      {
                                                         EffectiveVersion = specification.EffectiveVersion,
                                                         ReadOrder = @event.StorageInformation.ReadOrder,
                                                         InsertedVersion = @event.StorageInformation.InsertedVersion,
                                                         RefactoringInformation = @event.StorageInformation.RefactoringInformation
                                                      }));
               }
            }
         ));

   public EventNeighborhood LoadEventNeighborHood(Guid eventId)
      => _transactionLockManager.WithTransactionWideLock(
         _state.Update(state => state.Events.Single(@event => @event.EventId == eventId)).AggregateId,
         () => _state.Update(state =>
         {
            var found = state.Events.Single(it => it.EventId == eventId);

            var effectiveOrder = found.StorageInformation.ReadOrder!.Value;
            var previousEvent = state.Events
                                     .Where(it => it.StorageInformation.ReadOrder!.Value < effectiveOrder)
                                     .OrderByDescending(it => it.StorageInformation.ReadOrder)
                                     .FirstOrDefault();

            var nextEvent = state.Events
                                 .Where(it => it.StorageInformation.ReadOrder!.Value > effectiveOrder)
                                 .OrderBy(it => it.StorageInformation.ReadOrder)
                                 .FirstOrDefault();

            return new EventNeighborhood(effectiveReadOrder: effectiveOrder,
                                         previousEventReadOrder: previousEvent?.StorageInformation.ReadOrder,
                                         nextEventReadOrder: nextEvent?.StorageInformation.ReadOrder);
         }));

   public IEnumerable<EventDataRow> StreamEvents(int batchSize)
      => _state.Update(state => state.Events
                                     .OrderBy(@event => @event.StorageInformation.ReadOrder)
                                     .Where(@event => @event.StorageInformation.EffectiveVersion > 0)
                                     .ToArray());

   public IReadOnlyList<CreationEventRow> ListAggregateIdsInCreationOrder()
      => _state.Update(state =>
      {
         var found = new HashSet<Guid>();
         var result = new List<CreationEventRow>();
         foreach(var row in state.Events.Where(@event => @event.AggregateVersion == 1))
         {
            if(!found.Contains(row.AggregateId))
            {
               found.Add(row.AggregateId);
               result.Add(new CreationEventRow(aggregateId: row.AggregateId, typeId: row.EventType));
            }
         }

         return result;
      });

   public void DeleteAggregate(Guid aggregateId)
      => _transactionLockManager.WithTransactionWideLock(
         aggregateId,
         () => _state.Update(state => state.DeleteAggregate(aggregateId)));

   public void SetupSchemaIfDatabaseUnInitialized()
   { /*Nothing to do for an in-memory storage*/
   }

   class State
   {
      List<EventDataRow> _events = [];
      IThreadShared<State> _state = null!;

      public IReadOnlyList<EventDataRow> Events => TransactionalOverlay == null ? _events : _events.Concat(TransactionalOverlay).ToList();

      readonly Dictionary<string, List<EventDataRow>> _overlays = new();

      List<EventDataRow>? TransactionalOverlay
      {
         get
         {
            if(Transaction.Current == null) return null;

            var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
            return _overlays.GetOrAdd(transactionId,
                                      () =>
                                      {
                                         new VolatileLambdaTransactionParticipant(
                                               onCommit: () => _state.Update(state => state._events.AddRange(state._overlays[transactionId])),
                                               onTransactionCompleted: _ => _state.Update(state => state._overlays.Remove(transactionId)))
                                           .EnsureEnlistedInAnyAmbientTransaction();

                                         return new List<EventDataRow>();
                                      });
         }
      }

      public void ReplaceEvent(int index, EventDataRow row)
      {
         if(index < _events.Count)
         {
            _events[index] = row;
         } else
         {
            TransactionalOverlay![index - _events.Count] = row;
         }
      }

      public void AddRange(IEnumerable<EventDataRow> rows) =>
         TransactionalOverlay!.AddRange(rows);

      public void DeleteAggregate(Guid aggregateId) =>
         _events = _events.Where(row => row.AggregateId != aggregateId).ToList();

      public void Init(IThreadShared<State> @lock) =>
         _state = @lock;
   }
}