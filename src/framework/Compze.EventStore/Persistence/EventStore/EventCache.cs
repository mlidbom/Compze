using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using Compze.Contracts;
using Compze.Persistence.EventStore.PersistenceLayer;
using Compze.SystemCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;
using Compze.SystemCE.TransactionsCE;
using Microsoft.Extensions.Caching.Memory;

namespace Compze.Persistence.EventStore;

public interface IEventCache
{
   void Clear();
}

class EventCache : IDisposable, IEventCache
{
   class TransactionalOverlay(EventCache eventCache)
   {
      readonly EventCache _parent = eventCache;
      readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

      readonly IThreadShared<Dictionary<string, Dictionary<Guid, Entry>>> _overlays = ThreadShared.WithDefaultTimeout<Dictionary<string, Dictionary<Guid, Entry>>>();

      Dictionary<Guid, Entry> CurrentOverlay
      {
         get
         {
            Assert.State.NotNull(Transaction.Current);
            var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
            Dictionary<Guid, Entry>? overlay = null;

            if(_overlays.Update(it => it.TryGetValue(transactionId, out overlay)))
            {
               return Assert.Return.NotNull(overlay);
            }

            overlay = [];

            _overlays.Update(it => it.Add(transactionId, overlay));

            Transaction.Current.OnCommittedSuccessfully(() => _parent.AcceptTransactionResult(overlay));
            Transaction.Current.OnCompleted(() => _overlays.Update(it => it.Remove(transactionId)));

            return overlay;
         }
      }

      internal void Add(Guid aggregateId, Entry entry) => _monitor.Update(
         () => CurrentOverlay[aggregateId] = entry);

      internal bool TryGet(Guid aggregateId, [NotNullWhen(true)]out Entry? entry)
      {
         entry = null;
         if(Transaction.Current == null) return false;
         using(_monitor.TakeReadLock())
         {
            return CurrentOverlay.TryGetValue(aggregateId, out entry);
         }
      }
   }

   internal class Entry
   {
      public static readonly Entry Empty = new();
      Entry()
      {
         Events = [];
         MaxSeenInsertedVersion = 0;
      }

      public IReadOnlyList<AggregateEvent> Events { get; private set; }
      public int MaxSeenInsertedVersion { get; private set; }
      int InsertedVersionToAggregateVersionOffset { get; }

      public Entry(IReadOnlyList<AggregateEvent> events, int maxSeenInsertedVersion)
      {
         Events = events;
         MaxSeenInsertedVersion = maxSeenInsertedVersion;
         InsertedVersionToAggregateVersionOffset = MaxSeenInsertedVersion - events[^1].AggregateVersion;
      }

      public EventInsertionSpecification CreateInsertionSpecificationForNewEvent(IAggregateEvent @event)
      {
         if(InsertedVersionToAggregateVersionOffset > 0)
         {
            return new EventInsertionSpecification(@event: @event,
                                                   insertedVersion: @event.AggregateVersion + InsertedVersionToAggregateVersionOffset,
                                                   effectiveVersion:@event.AggregateVersion);
         } else
         {
            return new EventInsertionSpecification(@event:@event);
         }
      }
   }

   readonly TransactionalOverlay _transactionalOverlay;

   public EventCache()
   {
      _internalCache = new MemoryCache(new MemoryCacheOptions());
      _transactionalOverlay = new TransactionalOverlay(this);
   }

   void AcceptTransactionResult(Dictionary<Guid, Entry> overlay)
   {
      foreach(var (key, value) in overlay)
      {
         StoreInternal(key, value);
      }
   }

   public Entry Get(Guid id)
   {
      if(_transactionalOverlay.TryGet(id, out var entry))
      {
         return entry;
      }

      return GetInternal(id) ?? Entry.Empty;
   }

   public void Store(Guid id, Entry entry)
   {
      if(Transaction.Current != null)
      {
         _transactionalOverlay.Add(id, entry);
      } else
      {
         StoreInternal(id, entry);
      }
   }

   public void Remove(Guid id) => RemoveInternal(id);

   MemoryCache _internalCache;

   static readonly MemoryCacheEntryOptions Policy = new()
                                                    {
                                                       SlidingExpiration = 20.Minutes()
                                                    };

   void StoreInternal(Guid id, Entry entry) => _internalCache.Set(key: id.ToString(), value: entry, options: Policy);
   Entry? GetInternal(Guid id) => (Entry?)_internalCache.Get(id.ToString());
   void RemoveInternal(Guid id) => _internalCache.Remove(key: id.ToString());

   public void Clear()
   {
      var originalCache = _internalCache;
      _internalCache = new MemoryCache(new MemoryCacheOptions());
      originalCache.Dispose();
   }

   public void Dispose() => _internalCache.Dispose();
}