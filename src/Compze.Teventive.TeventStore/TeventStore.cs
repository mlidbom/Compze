using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Wiring.Registration;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Internal;
using Compze.Teventive.TeventStore.Abstractions.Internal.SqlLayer.Abstractions;
using Compze.Teventive.TeventStore.Abstractions.Public;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;
using Compze.Teventive.TeventStore.Refactoring.Migrations;
using Compze.TypeIdentifiers;
using JetBrains.Annotations;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.TeventStore;

#pragma warning disable CA1724 // Type name intentionally matches namespace concept
[UsedImplicitly] partial class TeventStore : ITeventStore
{
   readonly ITypeMap _typeMap;
   readonly ITeventStoreSerializer _serializer;
   static readonly ILogger Log = CompzeLogger.For<TeventStore>();

   readonly ITeventStoreSqlLayer _sqlLayer;

   readonly TeventCache _cache;
   readonly IReadOnlyList<ITeventMigration> _migrationFactories;

   internal static void RegisterWith(IComponentRegistrar registrar, Func<IReadOnlyList<ITeventMigration>> migrations)
      => registrar.Register(Scoped.For<ITeventStore>()
                                  .CreatedBy((ITeventStoreSqlLayer sqlLayer, ITypeMap typeMap, ITeventStoreSerializer serializer, TeventCache cache) =>
                                                new TeventStore(sqlLayer, typeMap, serializer, cache, migrations())));

   public TeventStore(ITeventStoreSqlLayer sqlLayer, ITypeMap typeMap, ITeventStoreSerializer serializer, TeventCache cache, IEnumerable<ITeventMigration> migrations)
   {
      _typeMap = typeMap;
      _serializer = serializer;
      Log.Debug("Constructor called");

      _migrationFactories = migrations.ToList();

      _cache = cache;
      _sqlLayer = sqlLayer;
   }

   public IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistoryForUpdate(TaggregateId id) => GetTaggregateHistoryInternal(taggregateId: id, takeWriteLock: true);

   public IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistory(TaggregateId id) => GetTaggregateHistoryInternal(id, takeWriteLock: false);

   IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> GetTaggregateHistoryInternal(TaggregateId taggregateId, bool takeWriteLock)
   {
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var cachedTaggregateHistory = _cache.Get(taggregateId);

      var newHistoryFromSqlLayer = GetTaggregateTeventsFromSqlLayer(taggregateId, takeWriteLock, cachedTaggregateHistory.MaxSeenInsertedVersion);

      if(newHistoryFromSqlLayer.Length == 0)
      {
         return cachedTaggregateHistory.WrappedTevents;
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

      var newTeventsFromSqlLayer = newHistoryFromSqlLayer.Select(it => it.WrappedTevent).ToArray();
      if(cachedTaggregateHistory.WrappedTevents.Count == 0)
      {
         TaggregateHistoryValidator.ValidateHistory(taggregateId, newTeventsFromSqlLayer);
      }

      var newTaggregateHistory = cachedTaggregateHistory.WrappedTevents.Count == 0
                                   ? SingleTaggregateInstanceTeventStreamMutator.MutateCompleteTaggregateHistory(_migrationFactories, newTeventsFromSqlLayer)
                                   : cachedTaggregateHistory.WrappedTevents.Concat(newTeventsFromSqlLayer)
                                                           .ToArray();

      if(cachedMigratedHistoryExists)
      {
         SingleTaggregateInstanceTeventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, newTaggregateHistory);
      }

      var maxSeenInsertedVersion = newHistoryFromSqlLayer.Max(tevent => tevent.StorageInformation.InsertedVersion);
      TaggregateHistoryValidator.ValidateHistory(taggregateId, newTaggregateHistory);
      _cache.Store(taggregateId, new TeventCache.Entry(wrappedTevents: newTaggregateHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));

      return newTaggregateHistory;
   }

   ITaggregateTevent<ITaggregateTevent> HydrateTevent(TeventDataRow teventDataRowRow)
   {
      var wrappedTevent = _serializer.Deserialize(wrapperTeventType: teventDataRowRow.TeventType.Type, json: teventDataRowRow.TeventJson);
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)wrappedTevent.Tevent).SetTaggregateIdInternal(teventDataRowRow.TaggregateId);
      ((IMutableTaggregateTevent)wrappedTevent.Tevent).SetTaggregateVersionInternal(teventDataRowRow.TaggregateVersion);
      ((IMutableTaggregateTevent)wrappedTevent.Tevent).SetTessageIdInternal(teventDataRowRow.TeventId);
      ((IMutableTaggregateTevent)wrappedTevent.Tevent).SetUtcTimeStampInternal(teventDataRowRow.UtcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
      return wrappedTevent;
   }

   TaggregateTeventWithRefactoringInformation[] GetTaggregateTeventsFromSqlLayer(TaggregateId taggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
      => _sqlLayer.GetTaggregateHistory(taggregateId: taggregateId,
                                               startAfterInsertedVersion: startAfterInsertedVersion,
                                               takeWriteLock: takeWriteLock)
                          .Select(it => new TaggregateTeventWithRefactoringInformation(HydrateTevent(it), it.StorageInformation))
                          .ToArray();

   static bool IsRefactoringTevent(TaggregateTeventWithRefactoringInformation tevent) => tevent.StorageInformation.RefactoringInformation != null;

   IEnumerable<ITaggregateTevent<ITaggregateTevent>> StreamTevents(int batchSize)
   {
      var streamMutator = CompleteTeventStoreStreamMutator.Create(_migrationFactories);
      return streamMutator.Mutate(_sqlLayer.StreamTevents(batchSize).Select(HydrateTevent));
   }

   public void StreamTevents(int batchSize, Action<IReadOnlyList<ITaggregateTevent<ITaggregateTevent>>> handleTevents)
   {
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var batches = StreamTevents(batchSize)
                   .ChopIntoSizesOf(batchSize)
                   .Select(batch => batch.ToList());
      foreach(var batch in batches)
      {
         handleTevents(batch);
      }
   }

   public void SaveSingleTaggregateTevents(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> wrappedTevents)
   {
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var taggregateId = wrappedTevents[0].Tevent.TaggregateId;

      if(wrappedTevents.Any(it => it.Tevent.TaggregateId != taggregateId))
      {
         throw new ArgumentException("Got tevents from multiple Taggregates. This is not supported.");
      }

      var cacheEntry = _cache.Get(taggregateId);
      var specifications = wrappedTevents.Select(cacheEntry.CreateInsertionSpecificationForNewTevent).ToArray();

      var teventRows = wrappedTevents
                     .Select(wrappedTevent => new TeventDataRow(specification: cacheEntry.CreateInsertionSpecificationForNewTevent(wrappedTevent), _typeMap.GetId(wrappedTevent.GetType()), teventAsJson: _serializer.Serialize(wrappedTevent)))
                     .ToList();

      teventRows.ForEach(it => it.StorageInformation.EffectiveVersion = it.TaggregateVersion);
      _sqlLayer.InsertSingleTaggregateTevents(teventRows);

      var completeTaggregateHistory = cacheEntry
                                    .WrappedTevents.Concat(wrappedTevents)
                                    .ToArray();
      SingleTaggregateInstanceTeventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeTaggregateHistory);
      TaggregateHistoryValidator.ValidateHistory(taggregateId, completeTaggregateHistory);

      _cache.Store(taggregateId,
                   new TeventCache.Entry(completeTaggregateHistory,
                                        maxSeenInsertedVersion: specifications.Max(specification => specification.InsertedVersion)));
   }

   public void DeleteTaggregate(TaggregateId taggregateId)
   {
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();
      _cache.Remove(taggregateId);
      _sqlLayer.DeleteTaggregate(taggregateId);
   }

   public IEnumerable<TaggregateId> StreamTaggregateIdsInCreationOrder(Type? teventType = null)
   {
      Argument.Assert(teventType == null || teventType.IsInterface && (typeof(ITaggregateTevent).IsAssignableFrom(teventType) || typeof(IPublisherTevent<ITaggregateTevent>).IsAssignableFrom(teventType)));

      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();
      //The rows store wrapper types, so an inner tevent type filter is translated: it matches every wrapping of that tevent type.
      var wrapperTypeToMatch = teventType == null ? null : PublisherTevent.WrapperTypeMatchingAllWrappingsOf(teventType);
      return _sqlLayer.ListTaggregateIdsInCreationOrder()
                              .Where(it => wrapperTypeToMatch == null || wrapperTypeToMatch.IsAssignableFrom(it.TypeId.Type))
                              .Select(it => it.TaggregateId);
   }

   public class TaggregateTeventWithRefactoringInformation(ITaggregateTevent<ITaggregateTevent> wrappedTevent, TaggregateTeventStorageInformation storageInformation)
   {
      internal ITaggregateTevent<ITaggregateTevent> WrappedTevent { get; } = wrappedTevent;
      internal TaggregateTeventStorageInformation StorageInformation { get; } = storageInformation;
   }

   public void Dispose() {}
}
