using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time;
using Compze.Sql.Common.TeventStore.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;
using Compze.Utilities.Contracts;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading;
using JetBrains.Annotations;

namespace Compze.Tessaging.Teventive.TeventStore;

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

   public IReadOnlyList<IAggregateTevent> GetAggregateHistoryForUpdate(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: true);

   public IReadOnlyList<IAggregateTevent> GetAggregateHistory(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId, takeWriteLock: false);

   IReadOnlyList<IAggregateTevent> GetAggregateHistoryInternal(Guid aggregateId, bool takeWriteLock)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var cachedAggregateHistory = _cache.Get(aggregateId);

      var newHistoryFromSqlLayer = GetAggregateTeventsFromSqlLayer(aggregateId, takeWriteLock, cachedAggregateHistory.MaxSeenInsertedVersion);

      if(newHistoryFromSqlLayer.Length == 0)
      {
         return cachedAggregateHistory.Tevents;
      }

      var newerMigratedTeventsExist = newHistoryFromSqlLayer.Where(IsRefactoringTevent).Any();

      var cachedMigratedHistoryExists = cachedAggregateHistory.MaxSeenInsertedVersion > 0;

      var migrationsHaveBeenPersistedWhileWeHeldTeventsInCache = cachedMigratedHistoryExists && newerMigratedTeventsExist;
      if(migrationsHaveBeenPersistedWhileWeHeldTeventsInCache)
      {
         _cache.Remove(aggregateId);
         // ReSharper disable once TailRecursiveCall clarity over micro optimizations any day.
         return GetAggregateHistoryInternal(aggregateId, takeWriteLock);
      }

      var newTeventsFromSqlLayer = newHistoryFromSqlLayer.Select(it => it.Tevent).ToArray();
      if(cachedAggregateHistory.Tevents.Count == 0)
      {
         AggregateHistoryValidator.ValidateHistory(aggregateId, newTeventsFromSqlLayer);
      }

      var newAggregateHistory = cachedAggregateHistory.Tevents.Count == 0
                                   ? SingleAggregateInstanceTeventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, newTeventsFromSqlLayer)
                                   : cachedAggregateHistory.Tevents.Concat(newTeventsFromSqlLayer)
                                                           .ToArray();

      if(cachedMigratedHistoryExists)
      {
         SingleAggregateInstanceTeventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, newAggregateHistory);
      }

      var maxSeenInsertedVersion = newHistoryFromSqlLayer.Max(@tevent => @tevent.StorageInformation.InsertedVersion);
      AggregateHistoryValidator.ValidateHistory(aggregateId, newAggregateHistory);
      _cache.Store(aggregateId, new TeventCache.Entry(tevents: newAggregateHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));

      return newAggregateHistory;
   }

   AggregateTevent HydrateTevent(TeventDataRow teventDataRowRow)
   {
      var @tevent = (AggregateTevent)_serializer.Deserialize(teventType: _typeMapper.GetType(new TypeId(teventDataRowRow.TeventType)), json: teventDataRowRow.TeventJson);
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateTevent)@tevent).SetAggregateIdInternal(teventDataRowRow.AggregateId);
      ((IMutableAggregateTevent)@tevent).SetAggregateVersionInternal(teventDataRowRow.AggregateVersion);
      ((IMutableAggregateTevent)@tevent).SetTessageIdInternal(teventDataRowRow.TeventId);
      ((IMutableAggregateTevent)@tevent).SetUtcTimeStampInternal(teventDataRowRow.UtcTimeStamp);
#pragma warning restore CS0618 // Type or member is obsolete
      return @tevent;
   }

   AggregateTeventWithRefactoringInformation[] GetAggregateTeventsFromSqlLayer(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
      => _sqlLayer.GetAggregateHistory(aggregateId: aggregateId,
                                               startAfterInsertedVersion: startAfterInsertedVersion,
                                               takeWriteLock: takeWriteLock)
                          .Select(it => new AggregateTeventWithRefactoringInformation(HydrateTevent(it), it.StorageInformation))
                          .ToArray();

   static bool IsRefactoringTevent(AggregateTeventWithRefactoringInformation @tevent) => @tevent.StorageInformation.RefactoringInformation != null;

   IEnumerable<IAggregateTevent> StreamTevents(int batchSize)
   {
      var streamMutator = CompleteTeventStoreStreamMutator.Create(_migrationFactories);
      return streamMutator.Mutate(_sqlLayer.StreamTevents(batchSize).Select(HydrateTevent));
   }

   public void StreamTevents(int batchSize, Action<IReadOnlyList<IAggregateTevent>> handleTevents)
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

   public void SaveSingleAggregateTevents(IReadOnlyList<IAggregateTevent> aggregateTevents)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();

      var aggregateId = aggregateTevents[0].AggregateId;

      if(aggregateTevents.Any(it => it.AggregateId != aggregateId))
      {
         throw new ArgumentException("Got tevents from multiple Aggregates. This is not supported.");
      }

      var cacheEntry = _cache.Get(aggregateId);
      var specifications = aggregateTevents.Select(@tevent => cacheEntry.CreateInsertionSpecificationForNewTevent(@tevent)).ToArray();

      var teventRows = aggregateTevents
                     .Select(@tevent => new TeventDataRow(specification: cacheEntry.CreateInsertionSpecificationForNewTevent(@tevent), _typeMapper.GetId(@tevent.GetType()).GuidValue, teventAsJson: _serializer.Serialize((AggregateTevent)@tevent)))
                     .ToList();

      teventRows.ForEach(it => it.StorageInformation.EffectiveVersion = it.AggregateVersion);
      _sqlLayer.InsertSingleAggregateTevents(teventRows);

      var completeAggregateHistory = cacheEntry
                                    .Tevents.Concat(aggregateTevents)
                                    .Cast<AggregateTevent>()
                                    .ToArray();
      SingleAggregateInstanceTeventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeAggregateHistory);
      AggregateHistoryValidator.ValidateHistory(aggregateId, completeAggregateHistory);

      _cache.Store(aggregateId,
                   new TeventCache.Entry(completeAggregateHistory,
                                        maxSeenInsertedVersion: specifications.Max(specification => specification.InsertedVersion)));
   }

   public void DeleteAggregate(Guid aggregateId)
   {
      _usageGuard.EnsureAccessValid();
      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();
      _cache.Remove(aggregateId);
      _sqlLayer.DeleteAggregate(aggregateId);
   }

   public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type? teventBaseType = null)
   {
      Assert.Argument.Is(teventBaseType == null || teventBaseType.IsInterface && typeof(IAggregateTevent).IsAssignableFrom(teventBaseType));
      _usageGuard.EnsureAccessValid();

      _sqlLayer.SetupSchemaIfDatabaseUnInitialized();
      return _sqlLayer.ListAggregateIdsInCreationOrder()
                              .Where(it => teventBaseType == null || teventBaseType.IsAssignableFrom(_typeMapper.GetType(new TypeId(it.TypeId))))
                              .Select(it => it.AggregateId);
   }

   class AggregateTeventWithRefactoringInformation(AggregateTevent tevent, AggregateTeventStorageInformation storageInformation)
   {
      internal AggregateTevent Tevent { get; } = tevent;
      internal AggregateTeventStorageInformation StorageInformation { get; } = storageInformation;
   }

   public void Dispose() {}
}
