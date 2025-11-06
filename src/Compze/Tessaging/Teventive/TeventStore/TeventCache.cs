using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.Teventive.TeventStore;

public interface ITeventCache
{
   void Clear();
}

class TeventCache : IDisposable, ITeventCache
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(
         Singleton.For<TeventCache, ITeventCache>()
                  .CreatedBy(() => new TeventCache()));

   TeventCache()
   {
      _internalCache = new MemoryCache(new MemoryCacheOptions());
      _transactionalOverlay = new TransactionalOverlay(this);
   }

   class TransactionalOverlay(TeventCache teventCache)
   {
      readonly TeventCache _parent = teventCache;
      readonly IMonitorCE _monitor = IMonitorCE.WithDefaultTimeout();

      readonly IThreadShared<Dictionary<string, Dictionary<TaggregateId, Entry>>> _overlays = IThreadShared.WithDefaultTimeout<Dictionary<string, Dictionary<TaggregateId, Entry>>>();

      Dictionary<TaggregateId, Entry> CurrentOverlay
      {
         get
         {
            Assert.State.NotNull(Transaction.Current);
            var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
            Dictionary<TaggregateId, Entry>? overlay = null;

            if(_overlays.Read(it => it.TryGetValue(transactionId, out overlay)))
            {
               return Assert.Result.NotNull(overlay).then(overlay);
            }

            overlay = [];

            _overlays.Update(it => it.Add(transactionId, overlay));

            Transaction.Current.OnCommittedSuccessfully(() => _parent.AcceptTransactionResult(overlay));
            Transaction.Current.OnCompleted(() => _overlays.Update(it => it.Remove(transactionId)));

            return overlay;
         }
      }

      internal void Add(TaggregateId taggregateId, Entry entry) => _monitor.Update(
         () => CurrentOverlay[taggregateId] = entry);

      internal bool TryGet(TaggregateId taggregateId, [NotNullWhen(true)]out Entry? entry)
      {
         entry = null;
         if(Transaction.Current == null) return false;
         using(_monitor.TakeReadLock())
         {
            return CurrentOverlay.TryGetValue(taggregateId, out entry);
         }
      }
   }

   internal class Entry
   {
      public static readonly Entry Empty = new();
      Entry()
      {
         Tevents = [];
         MaxSeenInsertedVersion = 0;
      }

      public IReadOnlyList<TaggregateTevent> Tevents { get; private set; }
      public int MaxSeenInsertedVersion { get; private set; }
      int InsertedVersionToTaggregateVersionOffset { get; }

      public Entry(IReadOnlyList<TaggregateTevent> tevents, int maxSeenInsertedVersion)
      {
         Tevents = tevents;
         MaxSeenInsertedVersion = maxSeenInsertedVersion;
         InsertedVersionToTaggregateVersionOffset = MaxSeenInsertedVersion - tevents[^1].TaggregateVersion;
      }

      public TeventInsertionSpecification CreateInsertionSpecificationForNewTevent(ITaggregateTevent tevent)
      {
         if(InsertedVersionToTaggregateVersionOffset > 0)
         {
            return new TeventInsertionSpecification(tevent: tevent.ToTaggregateTeventData(),
                                                   insertedVersion: tevent.TaggregateVersion + InsertedVersionToTaggregateVersionOffset,
                                                   effectiveVersion:tevent.TaggregateVersion);
         } else
         {
            return new TeventInsertionSpecification(tevent:tevent.ToTaggregateTeventData());
         }
      }
   }

   readonly TransactionalOverlay _transactionalOverlay;

   void AcceptTransactionResult(Dictionary<TaggregateId, Entry> overlay)
   {
      foreach(var (key, value) in overlay)
      {
         StoreInternal(key, value);
      }
   }

   public Entry Get(TaggregateId id)
   {
      if(_transactionalOverlay.TryGet(id, out var entry))
      {
         return entry;
      }

      return GetInternal(id) ?? Entry.Empty;
   }

   public void Store(TaggregateId id, Entry entry)
   {
      if(Transaction.Current != null)
      {
         _transactionalOverlay.Add(id, entry);
      } else
      {
         StoreInternal(id, entry);
      }
   }

   public void Remove(TaggregateId id) => RemoveInternal(id);

   MemoryCache _internalCache;

   static readonly MemoryCacheEntryOptions Policy = new()
                                                    {
                                                       SlidingExpiration = 20.Minutes()
                                                    };

   void StoreInternal(TaggregateId id, Entry entry) => _internalCache.Set(key: id.ToString(), value: entry, options: Policy);
   Entry? GetInternal(TaggregateId id) => (Entry?)_internalCache.Get(id.ToString());
   void RemoveInternal(TaggregateId id) => _internalCache.Remove(key: id.ToString());

   public void Clear()
   {
      var originalCache = _internalCache;
      _internalCache = new MemoryCache(new MemoryCacheOptions());
      originalCache.Dispose();
   }

   public void Dispose() => _internalCache.Dispose();
}