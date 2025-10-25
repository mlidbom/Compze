using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Sql.Common.TeventStore.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using ReadOrder = Compze.Sql.Common.TeventStore.Abstractions.ReadOrder;

namespace Compze.Tessaging.Teventive.TeventStore;

partial class TeventStore
{
   public void PersistMigrations()
   {
      Assert.State.Is(Transaction.Current == null, () => $"Cannot run {nameof(PersistMigrations)} within a transaction. Internally manages transactions.");
      Log.Info("Starting to persist migrations");

      long migratedAggregates = 0;
      long updatedAggregates = 0;
      long newTeventCount = 0;
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
                  //performance: Look at ways to avoid taking a lock for a long time as we do now. This might be a problem in production.
                  using var transaction = new TransactionScope(TransactionScopeOption.Required, scopeTimeout: 10.Minutes());

                  var original = GetAggregateTeventsFromSqlLayer(aggregateId: aggregateId, takeWriteLock: true);

                  var highestSeenVersion = original.Max(@tevent => @tevent.StorageInformation.InsertedVersion) + 1;

                  var updatedAggregatesBeforeMigrationOfThisAggregate = updatedAggregates;

                  var refactorings = new List<List<TeventDataRow>>();

                  var inMemoryMigratedHistory = SingleAggregateInstanceTeventStreamMutator.MutateCompleteAggregateHistory(
                     _migrationFactories,
                     original.Select(it => it.Tevent).ToArray(),
                     newTevents =>
                     {
                        //Make sure we don't try to insert into an occupied InsertedVersion
                        newTevents.ForEach(refactoredTevent =>
                        {
                           refactoredTevent.StorageInformation.InsertedVersion = highestSeenVersion++;
                        });

                        refactorings.Add(newTevents
                                        .Select(it => new TeventDataRow(@tevent: it.NewTevent.ToAggregateTeventData(),
                                                                          it.StorageInformation,
                                                                          _typeMapper.GetId(it.NewTevent.GetType()).GuidValue,
                                                                          teventAsJson: _serializer.Serialize(it.NewTevent)))
                                        .ToList());

                        updatedAggregates = updatedAggregatesBeforeMigrationOfThisAggregate + 1;
                        newTeventCount += newTevents.Count;
                     });

                  if(refactorings.Count > 0)
                  {
                     refactorings.ForEach(InsertTeventsForSingleRefactoring);

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

            Log.Info($"{PercentDone()}% done. Inspected: {migratedAggregates} / {aggregateIdsInCreationOrder.Count}, Updated: {updatedAggregates}, New Tevents: {newTeventCount}");
         }
      }

      Log.Info("Done persisting migrations.");
      Log.Info($"Inspected: {migratedAggregates} , Updated: {updatedAggregates}, New Tevents: {newTeventCount}");
      if(exceptions.Any())
      {
         throw new AggregateException($"""

                                       Failed to persist {exceptions.Count} migrations. 

                                       AggregateIds: 
                                       {exceptions.Select(it => it.AggregateId.ToString()).Join($",{Environment.NewLine}")}
                                       """, exceptions.Select(it => it.Exception));
      }

   }

   void FixManualVersions(AggregateTeventWithRefactoringInformation[] originalHistory, AggregateTevent[] newHistory, IReadOnlyList<List<TeventDataRow>> refactorings)
   {
      var versionUpdates = new List<VersionSpecification>();
      var replacedOrRemoved = originalHistory.Where(it => newHistory.None(@tevent => @tevent.TessageId == it.Tevent.TessageId)).ToList();
      versionUpdates.AddRange(replacedOrRemoved.Select(it => new VersionSpecification(it.Tevent.TessageId, -it.StorageInformation.EffectiveVersion)));

      var replacedOrRemoved2 = refactorings.SelectMany(it =>it).Where(it => newHistory.None(@tevent => @tevent.TessageId == it.TeventId));
      versionUpdates.AddRange(replacedOrRemoved2.Select(it => new VersionSpecification(it.TeventId, -it.StorageInformation.EffectiveVersion)));

      //Performance: Filter out rows where the new value equals the old value. We don't want to go updating every tevent in every refactored aggregate if only a few, or none, have actually changed.
      versionUpdates.AddRange(newHistory.Select((it , index) => new VersionSpecification(it.TessageId, index + 1)));

      _sqlLayer.UpdateEffectiveVersions(versionUpdates);
   }

   void AssertHistoriesAreIdentical(AggregateTevent[] inMemoryMigratedHistory, IReadOnlyList<IAggregateTevent> loadedAggregateHistory)
   {
      Assert.Result.Is(inMemoryMigratedHistory.Length == loadedAggregateHistory.Count);
      for(var index = 0; index < inMemoryMigratedHistory.Length; ++index)
      {
         var inMemory = inMemoryMigratedHistory[index];
         var loaded = loadedAggregateHistory[index];
         Assert.Result
               .Is(inMemory.AggregateId == loaded.AggregateId)
               .Is(inMemory.TessageId == loaded.TessageId)
               .Is(inMemory.AggregateVersion == loaded.AggregateVersion)
               .Is(inMemory.UtcTimeStamp == loaded.UtcTimeStamp)
               .Is(inMemory.GetType() == loaded.GetType())
               .Is(_serializer.Serialize(inMemory) == _serializer.Serialize((AggregateTevent)loaded));
      }
   }

   void InsertTeventsForSingleRefactoring(IReadOnlyList<TeventDataRow> tevents)
   {
      var refactoring = tevents[0].StorageInformation.RefactoringInformation!;

      switch(refactoring.RefactoringType)
      {
         case AggregateTeventRefactoringType.Replace:
            ReplaceTevent(refactoring.TargetTevent, tevents.ToArray());
            break;
         case AggregateTeventRefactoringType.InsertBefore:
            InsertBeforeTevent(refactoring.TargetTevent, tevents.ToArray());
            break;
         case AggregateTeventRefactoringType.InsertAfter:
            InsertAfterTevent(refactoring.TargetTevent, tevents.ToArray());
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   void InsertAfterTevent(Guid teventId, TeventDataRow[] insertAfterGroup)
   {
      var teventToInsertAfter = _sqlLayer.LoadTeventNeighborHood(teventId);

      SetManualReadOrders(newTevents: insertAfterGroup,
                          rangeStart: teventToInsertAfter.EffectiveReadOrder,
                          rangeEnd: teventToInsertAfter.NextTeventReadOrder);

      _sqlLayer.InsertSingleAggregateTevents(insertAfterGroup);
   }

   void InsertBeforeTevent(Guid teventId, TeventDataRow[] insertBefore)
   {
      var teventToInsertBefore = _sqlLayer.LoadTeventNeighborHood(teventId);

      SetManualReadOrders(newTevents: insertBefore,
                          rangeStart: teventToInsertBefore.PreviousTeventReadOrder,
                          rangeEnd: teventToInsertBefore.EffectiveReadOrder);

      _sqlLayer.InsertSingleAggregateTevents(insertBefore);
   }

   void ReplaceTevent(Guid teventId, TeventDataRow[] replacementTevents)
   {
      var neighborHood = _sqlLayer.LoadTeventNeighborHood(teventId);

      //We are not making maximally efficient use of the space here. Since the replaced tevent is no longer in use we should theoretically be able to start the range at the previous tevents position.
      //To make this possible without a collision on the unique index the replaced tevents read order would need to be moved out of the way somehow. Negating it seems easy but actually introduces risk of collisions.
      //Replacing an tevent that had previously replaced another tevent would be likely to result in trying to save the same negative (removed) read order again.
      //Fixing this seems rather non-trivial, so for now we keep the read orders of replaced tevents in place and accept that we do not use the space optimally.
      //Removing the unique constraint would work, but would make us more vulnerable to data corruption issues.
      SetManualReadOrders(newTevents: replacementTevents,
                          rangeStart: neighborHood.EffectiveReadOrder,
                          rangeEnd: neighborHood.NextTeventReadOrder);

      _sqlLayer.InsertSingleAggregateTevents(replacementTevents);
   }

   static void SetManualReadOrders(TeventDataRow[] newTevents, ReadOrder rangeStart, ReadOrder rangeEnd)
   {
      var readOrders = ReadOrder.CreateOrdersForTeventsBetween(newTevents.Length, rangeStart, rangeEnd);
      for (var index = 0; index < newTevents.Length; index++)
      {
         newTevents[index].StorageInformation.ReadOrder = readOrders[index];
      }
   }

   static bool IsRecoverableSqlException(Exception exception)
   {
      var tessage = exception.Message.ToUpperInvariant();
      return tessage.ContainsInvariant("TIMEOUT") || tessage.ContainsInvariant("DEADLOCK");
   }
}