using System.Diagnostics.CodeAnalysis;
using System.Transactions;
using Compze.Abstractions;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions._internal.SqlLayer.Abstractions;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Microsoft.Extensions.Caching.Memory;
using Compze.Teventive.TeventStore._private;

namespace Compze.Teventive.TeventStore;

public interface ITeventCache
{
   void Clear();
}

public class TeventCache : IDisposable, ITeventCache
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
      readonly IMonitor _monitor = IMonitor.New();

      readonly IThreadShared<Dictionary<string, Dictionary<TaggregateId, Entry>>> _overlays = IThreadShared.New(new Dictionary<string, Dictionary<TaggregateId, Entry>>());

      Dictionary<TaggregateId, Entry> CurrentOverlay
      {
         get
         {
            Contract.State.NotNull(Transaction.Current);
            var transactionId = Transaction.Current.TransactionInformation.LocalIdentifier;
            Dictionary<TaggregateId, Entry>? overlay = null;

            if(_overlays.Locked(it => it.TryGetValue(transactionId, out overlay)))
            {
               return overlay._assert().NotNull();
            }

            overlay = [];

            _overlays.Locked(it => it.Add(transactionId, overlay));

            Transaction.Current.OnCommittedSuccessfully(() => _parent.AcceptTransactionResult(overlay));
            Transaction.Current.OnCompleted(() => _overlays.Locked(it => it.Remove(transactionId)));

            return overlay;
         }
      }

      internal void Add(TaggregateId taggregateId, Entry entry) => _monitor.Locked(
         () => CurrentOverlay[taggregateId] = entry);

      internal bool TryGet(TaggregateId taggregateId, [NotNullWhen(true)]out Entry? entry)
      {
         entry = null;
         if(Transaction.Current == null) return false;
         using(_monitor.TakeLock())
         {
            return CurrentOverlay.TryGetValue(taggregateId, out entry);
         }
      }
   }

   public class Entry
   {
      internal static readonly Entry Empty = new();
      Entry()
      {
         WrappedTevents = [];
         MaxSeenInsertedVersion = 0;
      }

      internal IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> WrappedTevents { get; private set; }
      internal int MaxSeenInsertedVersion { get; private set; }
      int InsertedVersionToTaggregateVersionOffset { get; }

      internal Entry(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> wrappedTevents, int maxSeenInsertedVersion)
      {
         WrappedTevents = wrappedTevents;
         MaxSeenInsertedVersion = maxSeenInsertedVersion;
         InsertedVersionToTaggregateVersionOffset = MaxSeenInsertedVersion - wrappedTevents[^1].Tevent.TaggregateVersion;
      }

      internal TeventInsertionSpecification CreateInsertionSpecificationForNewTevent(ITaggregateTevent<ITaggregateTevent> wrappedTevent)
      {
         if(InsertedVersionToTaggregateVersionOffset > 0)
         {
            return new TeventInsertionSpecification(tevent: wrappedTevent.Tevent.ToTaggregateTeventData(),
                                                   insertedVersion: wrappedTevent.Tevent.TaggregateVersion + InsertedVersionToTaggregateVersionOffset,
                                                   effectiveVersion:wrappedTevent.Tevent.TaggregateVersion);
         } else
         {
            return new TeventInsertionSpecification(tevent:wrappedTevent.Tevent.ToTaggregateTeventData());
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