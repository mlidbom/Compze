using System.Transactions;
using Compze.Abstractions.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions;
using Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;
using Compze.Contracts;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using ReadOrder = Compze.Core.Tessaging.Teventive.TeventStore.Internal.SqlLayer.Abstractions.ReadOrder;

namespace Compze.Tessaging.Teventive.TeventStore;

public partial class TeventStore
{
   public void PersistMigrations()
   {
      Contract.State.Assert(Transaction.Current == null, () => $"Cannot run {nameof(PersistMigrations)} within a transaction. Internally manages transactions.");
      Log.Info("Starting to persist migrations");

      long migratedTaggregates = 0;
      long updatedTaggregates = 0;
      long newTeventCount = 0;
      var logInterval = 1.Minutes();
      var lastLogTime = DateTime.UtcNow;

      const int recoverableErrorRetriesToMake = 5;
      var exceptions = new List<(TaggregateId TaggregateId,Exception Exception)>();

      var taggregateIdsInCreationOrder = StreamTaggregateIdsInCreationOrder().ToList();

      foreach (var taggregateId in taggregateIdsInCreationOrder)
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

                  var original = GetTaggregateTeventsFromSqlLayer(taggregateId: taggregateId, takeWriteLock: true);

                  var highestSeenVersion = original.Max(tevent => tevent.StorageInformation.InsertedVersion) + 1;

                  var updatedTaggregatesBeforeMigrationOfThisTaggregate = updatedTaggregates;

                  var refactorings = new List<List<TeventDataRow>>();

                  var inMemoryMigratedHistory = SingleTaggregateInstanceTeventStreamMutator.MutateCompleteTaggregateHistory(
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
                                        .Select(it => new TeventDataRow(tevent: it.NewTevent.ToTaggregateTeventData(),
                                                                          it.StorageInformation,
                                                                          LeafStorageGuid(_typeMap.GetId(it.NewTevent.GetType())),
                                                                          teventAsJson: _serializer.Serialize(it.NewTevent)))
                                        .ToList());

                        updatedTaggregates = updatedTaggregatesBeforeMigrationOfThisTaggregate + 1;
                        newTeventCount += newTevents.Count;
                     });

                  if(refactorings.Count > 0)
                  {
                     refactorings.ForEach(InsertTeventsForSingleRefactoring);

                     FixManualVersions(original, inMemoryMigratedHistory, refactorings);

                     var loadedTaggregateHistory = GetTaggregateHistoryInternal(taggregateId, takeWriteLock:false);
                     TaggregateHistoryValidator.ValidateHistory(taggregateId, loadedTaggregateHistory);
                     AssertHistoriesAreIdentical(inMemoryMigratedHistory, loadedTaggregateHistory);
                  }

                  migratedTaggregates++;
                  succeeded = true;
                  transaction.Complete();
               }
               catch(Exception e) when(IsRecoverableSqlException(e) && ++retries <= recoverableErrorRetriesToMake)
               {
                  Log.Warning(e, $"Failed to persist migrations for taggregate: {taggregateId}. Exception appears to be recoverable so running retry {retries} out of {recoverableErrorRetriesToMake}");
               }
            }
         }
#pragma warning disable CA1031 //We are gathering the exceptions to throw all together in an aggregate exception
         catch(Exception exception)
         {
#pragma warning restore CA1031
            Log.Error(exception, $"Failed to persist migrations for taggregate: {taggregateId}");
            exceptions.Add((taggregateId, exception));
         }

         if(logInterval < DateTime.UtcNow - lastLogTime)
         {
            lastLogTime = DateTime.UtcNow;
            // ReSharper disable once AccessToModifiedClosure
            int PercentDone() => (int)(double)migratedTaggregates / taggregateIdsInCreationOrder.Count * 100;

            Log.Info($"{PercentDone()}% done. Inspected: {migratedTaggregates} / {taggregateIdsInCreationOrder.Count}, Updated: {updatedTaggregates}, New Tevents: {newTeventCount}");
         }
      }

      Log.Info("Done persisting migrations.");
      Log.Info($"Inspected: {migratedTaggregates} , Updated: {updatedTaggregates}, New Tevents: {newTeventCount}");
      if(exceptions.Any())
      {
         throw new AggregateException($"""

                                       Failed to persist {exceptions.Count} migrations. 

                                       TaggregateIds: 
                                       {exceptions.Select(it => it.TaggregateId.ToString()).Join($",{Environment.NewLine}")}
                                       """, exceptions.Select(it => it.Exception));
      }

   }

   void FixManualVersions(TaggregateTeventWithRefactoringInformation[] originalHistory, TaggregateTevent[] newHistory, IReadOnlyList<List<TeventDataRow>> refactorings)
   {
      var versionUpdates = new List<VersionSpecification>();
      var replacedOrRemoved = originalHistory.Where(it => newHistory.None(tevent => tevent.Id == it.Tevent.Id)).ToList();
      versionUpdates.AddRange(replacedOrRemoved.Select(it => new VersionSpecification(it.Tevent.Id, -it.StorageInformation.EffectiveVersion)));

      var replacedOrRemoved2 = refactorings.SelectMany(it =>it).Where(it => newHistory.None(tevent => tevent.Id == it.TeventId));
      versionUpdates.AddRange(replacedOrRemoved2.Select(it => new VersionSpecification(it.TeventId, -it.StorageInformation.EffectiveVersion)));

      //Performance: Filter out rows where the new value equals the old value. We don't want to go updating every tevent in every refactored taggregate if only a few, or none, have actually changed.
      versionUpdates.AddRange(newHistory.Select((it , index) => new VersionSpecification(it.Id, index + 1)));

      _sqlLayer.UpdateEffectiveVersions(versionUpdates);
   }

   void AssertHistoriesAreIdentical(TaggregateTevent[] inMemoryMigratedHistory, IReadOnlyList<ITaggregateTevent> loadedTaggregateHistory)
   {
      Contract.Argument.Assert(inMemoryMigratedHistory.Length == loadedTaggregateHistory.Count);
      for(var index = 0; index < inMemoryMigratedHistory.Length; ++index)
      {
         var inMemory = inMemoryMigratedHistory[index];
         var loaded = loadedTaggregateHistory[index];
         inMemory._assert(it => it.TaggregateId == loaded.TaggregateId)
                 ._assert(it => it.Id == loaded.Id)
                 ._assert(it => it.TaggregateVersion == loaded.TaggregateVersion)
                 ._assert(it => it.UtcTimeStamp == loaded.UtcTimeStamp)
                 ._assert(it => it.GetType() == loaded.GetType())
                 ._assert(it => _serializer.Serialize(it) == _serializer.Serialize((TaggregateTevent)loaded));
      }
   }

   void InsertTeventsForSingleRefactoring(IReadOnlyList<TeventDataRow> tevents)
   {
      var refactoring = tevents[0].StorageInformation.RefactoringInformation!;

      switch(refactoring.RefactoringType)
      {
         case TaggregateTeventRefactoringType.Replace:
            ReplaceTevent(refactoring.TargetTevent, tevents.ToArray());
            break;
         case TaggregateTeventRefactoringType.InsertBefore:
            InsertBeforeTevent(refactoring.TargetTevent, tevents.ToArray());
            break;
         case TaggregateTeventRefactoringType.InsertAfter:
            InsertAfterTevent(refactoring.TargetTevent, tevents.ToArray());
            break;
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   void InsertAfterTevent(TessageId teventId, TeventDataRow[] insertAfterGroup)
   {
      var teventToInsertAfter = _sqlLayer.LoadTeventNeighborHood(teventId);

      SetManualReadOrders(newTevents: insertAfterGroup,
                          rangeStart: teventToInsertAfter.EffectiveReadOrder,
                          rangeEnd: teventToInsertAfter.NextTeventReadOrder);

      _sqlLayer.InsertSingleTaggregateTevents(insertAfterGroup);
   }

   void InsertBeforeTevent(TessageId teventId, TeventDataRow[] insertBefore)
   {
      var teventToInsertBefore = _sqlLayer.LoadTeventNeighborHood(teventId);

      SetManualReadOrders(newTevents: insertBefore,
                          rangeStart: teventToInsertBefore.PreviousTeventReadOrder,
                          rangeEnd: teventToInsertBefore.EffectiveReadOrder);

      _sqlLayer.InsertSingleTaggregateTevents(insertBefore);
   }

   void ReplaceTevent(TessageId teventId, TeventDataRow[] replacementTevents)
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

      _sqlLayer.InsertSingleTaggregateTevents(replacementTevents);
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
      return tessage.ContainsOrdinal("TIMEOUT") || tessage.ContainsOrdinal("DEADLOCK");
   }
}