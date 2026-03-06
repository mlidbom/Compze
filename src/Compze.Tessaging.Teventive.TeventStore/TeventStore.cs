using Compze.Abstractions.Public;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Core.Serialization.Internal;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.UsageGuards;
using JetBrains.Annotations;
using Compze.Contracts;
using static Compze.Contracts.Contract;

namespace Compze.Tessaging.Teventive.TeventStore;

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
[UsedImplicitly] partial class TeventStore : ITeventStore
{
   readonly ITypeMapper _typeMapper;
   readonly ITeventStoreSerializer _serializer;
   static readonly ILogger Log = CompzeLogger.For<TeventStore>();

   readonly SingleThreadUseGuard _usageGuard;

   readonly ITeventStoreSqlLayer _sqlLayer;

   readonly TeventCache _cache;
   readonly IReadOnlyList<ITeventMigration> _migrationFactories;

   internal static void RegisterWith(IComponentRegistrar registrar, Func<IReadOnlyList<ITeventMigration>> migrations)
      => registrar.Register(Scoped.For<ITeventStore>()
                                  .CreatedBy((ITeventStoreSqlLayer sqlLayer, ITypeMapper typeMapper, ITeventStoreSerializer serializer, TeventCache cache) =>
                                                new TeventStore(sqlLayer, typeMapper, serializer, cache, migrations())));

   public TeventStore(ITeventStoreSqlLayer sqlLayer, ITypeMapper typeMapper, ITeventStoreSerializer serializer, TeventCache cache, IEnumerable<ITeventMigration> migrations)
   {
      _typeMapper = typeMapper;
      _serializer = serializer;
      Log.Debug("Constructor called");

      _migrationFactories = migrations.ToList();

      _usageGuard = new SingleThreadUseGuard(this);
      _cache = cache;
      _sqlLayer = sqlLayer;
   }

   public IReadOnlyList<ITaggregateTevent> GetTaggregateHistoryForUpdate(TaggregateId id) => GetTaggregateHistoryInternal(taggregateId: id, takeWriteLock: true);

   public IReadOnlyList<ITaggregateTevent> GetTaggregateHistory(TaggregateId id) => GetTaggregateHistoryInternal(id, takeWriteLock: false);

   IReadOnlyList<ITaggregateTevent> GetTaggregateHistoryInternal(TaggregateId taggregateId, bool takeWriteLock)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var cachedTaggregateHistory = _cache.Get(taggregateId);

      var newHistoryFromSqlLayer = GetTaggregateTeventsFromSqlLayer(taggregateId, takeWriteLock, cachedTaggregateHistory.MaxSeenInsertedVersion);

      if(newHistoryFromSqlLayer.Length == 0)
      {
         return cachedTaggregateHistory.Tevents;
      }

      var newerMigratedTeventsExist = newHistoryFromSqlLayer.Where(IsRefactoringTevent).Any();

      var cachedMigratedHistoryExists = cachedTaggregateHistory.MaxSeenInsertedVersion > 0;

      var migrationsHaveBeenPersistedWhileWeHeldTeventsInCache = cachedMigratedHistoryExists && newerMigratedTeventsExist;
      if(migrationsHaveBeenPersistedWhileWeHeldTeventsInCache)
      {
         _cache.Remove(taggregateId);
         // ReSharper disable once TailRecursiveCall clarity over micro optimizations any day.
         return GetTaggregateHistoryInternal(taggregateId, takeWriteLock);
      }

      var newTeventsFromSqlLayer = newHistoryFromSqlLayer.Select(it => it.Tevent).ToArray();
      if(cachedTaggregateHistory.Tevents.Count == 0)
      {
         TaggregateHistoryValidator.ValidateHistory(taggregateId, newTeventsFromSqlLayer);
      }

      var newTaggregateHistory = cachedTaggregateHistory.Tevents.Count == 0
                                   ? SingleTaggregateInstanceTeventStreamMutator.MutateCompleteTaggregateHistory(_migrationFactories, newTeventsFromSqlLayer)
                                   : cachedTaggregateHistory.Tevents.Concat(newTeventsFromSqlLayer)
                                                           .ToArray();

      if(cachedMigratedHistoryExists)
      {
         SingleTaggregateInstanceTeventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, newTaggregateHistory);
      }

      var maxSeenInsertedVersion = newHistoryFromSqlLayer.Max(tevent => tevent.StorageInformation.InsertedVersion);
      TaggregateHistoryValidator.ValidateHistory(taggregateId, newTaggregateHistory);
      _cache.Store(taggregateId, new TeventCache.Entry(tevents: newTaggregateHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));

      return newTaggregateHistory;
   }

   TaggregateTevent HydrateTevent(TeventDataRow teventDataRowRow)
   {
      var tevent = (TaggregateTevent)_serializer.Deserialize(teventType: _typeMapper.GetType(teventDataRowRow.TeventType), json: teventDataRowRow.TeventJson);
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)tevent).SetTaggregateIdInternal(teventDataRowRow.TaggregateId);
      ((IMutableTaggregateTevent)tevent).SetTaggregateVersionInternal(teventDataRowRow.TaggregateVersion);
      ((IMutableTaggregateTevent)tevent).SetTessageIdInternal(teventDataRowRow.TeventId);
      ((IMutableTaggregateTevent)tevent).SetUtcTimeStampInternal(teventDataRowRow.UtcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
      return tevent;
   }

   TaggregateTeventWithRefactoringInformation[] GetTaggregateTeventsFromSqlLayer(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
      => _sqlLayer.GetTaggregateHistory(taggregateId: taggregateId,
                                               startAfterInsertedVersion: startAfterInsertedVersion,
                                               takeWriteLock: takeWriteLock)
                          .Select(it => new TaggregateTeventWithRefactoringInformation(HydrateTevent(it), it.StorageInformation))
                          .ToArray();

   static bool IsRefactoringTevent(TaggregateTeventWithRefactoringInformation tevent) => tevent.StorageInformation.RefactoringInformation != null;

   IEnumerable<ITaggregateTevent> StreamTevents(int batchSize)
   {
      var streamMutator = CompleteTeventStoreStreamMutator.Create(_migrationFactories);
      return streamMutator.Mutate(_sqlLayer.StreamTevents(batchSize).Select(HydrateTevent));
   }

   public void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent>> handleTevents)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var batches = StreamTevents(batchSize)
                   .ChopIntoSizesOf(batchSize)
                   .Select(batch => batch.ToList());
      foreach(var batch in batches)
      {
         handleTevents(batch);
      }
   }

   public void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent> tevents)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var taggregateId = tevents[0].TaggregateId;

      if(tevents.Any(it => it.TaggregateId != taggregateId))
      {
         throw new ArgumentException("Got tevents from multiple Taggregates. This is not supported.");
      }

      var cacheEntry = _cache.Get(taggregateId);
      var specifications = tevents.Select(tevent => cacheEntry.CreateInsertionSpecificationForNewTevent(tevent)).ToArray();

      var teventRows = tevents
                     .Select(tevent => new TeventDataRow(specification: cacheEntry.CreateInsertionSpecificationForNewTevent(tevent), _typeMapper.GetId(tevent.GetType()), teventAsJson: _serializer.Serialize((TaggregateTevent)tevent)))
                     .ToList();

      teventRows.ForEach(it => it.StorageInformation.EffectiveVersion = it.TaggregateVersion);
      _sqlLayer.InsertSingleTaggregateTevents(teventRows);

      var completeTaggregateHistory = cacheEntry
                                    .Tevents.Concat(tevents)
                                    .Cast<TaggregateTevent>()
                                    .ToArray();
      SingleTaggregateInstanceTeventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeTaggregateHistory);
      TaggregateHistoryValidator.ValidateHistory(taggregateId, completeTaggregateHistory);

      _cache.Store(taggregateId,
                   new TeventCache.Entry(completeTaggregateHistory,
                                        maxSeenInsertedVersion: specifications.Max(specification => specification.InsertedVersion)));
   }

   public void DeleteTaggregate(TaggregateId taggregateId)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();
      _cache.Remove(taggregateId);
      _sqlLayer.DeleteTaggregate(taggregateId);
   }

   public IEnumerable<TaggregateId> StreamTaggregateIdsInCreationOrder(Type? teventType = null)
   {
      Argument.Assert(teventType == null || teventType.IsInterface && typeof(ITaggregateTevent).IsAssignableFrom(teventType));
      _usageGuard.EnsureAccessValid();

      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();
      return _sqlLayer.ListTaggregateIdsInCreationOrder()
                              .Where(it => teventType == null || teventType.IsAssignableFrom(_typeMapper.GetType(new TypeId(it.TypeId))))
                              .Select(it => it.TaggregateId);
   }

   public class TaggregateTeventWithRefactoringInformation(TaggregateTevent tevent, TaggregateTeventStorageInformation storageInformation)
   {
      internal TaggregateTevent Tevent { get; } = tevent;
      internal TaggregateTeventStorageInformation StorageInformation { get; } = storageInformation;
   }

   public void Dispose() {}
}
