using Compze.Abstractions.Public;
using Compze.Abstractions.Time.Public;
using Compze.Hosting.Testing.Wiring;
using Compze.Tests.Common;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.DependencyInjection;
using Compze.Tests.Infrastructure;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using Compze.Tessaging.Abstractions;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations.Tevents;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Public;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable AccessToModifiedClosure

namespace Compze.Tests.Integration.CQRS.TeventRefactoring.Migrations;

//Todo: Write tests that verify that none of the sql layers lose precision in the persisted ReadOrder when persisting refactorings.
//todo: this file is more than 600 lines long. Break it up for sanity's sake.
public class TeventMigrationTest : TeventMigrationTestBase
{
   [PCT]
   public async Task Base_class_method_should_detect_incorrect_type_order()
   {
      await this.InvokingAsync(_ => RunMigrationTestAsync(
                                  new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, Ef, E2, Ef>())))
                .Must().ThrowAsync<Exception>();
   }

   [PCT]
   public async Task Replacing_E1_with_E2()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E2, Ef, Ef>(),
                                     Replace<E1>.With<E2>()));
   }

   [PCT]
   public async Task Replacing_E1_with_E2_at_end_of_stream()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1>(),
                                     EnumerableCE.OfTypes<Ec1, E2>(),
                                     Replace<E1>.With<E2>()));
   }

   [PCT]
   public async Task Replacing_E1_with_E2_E3_at_end_of_stream()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1>(),
                                     EnumerableCE.OfTypes<Ec1, E2, E3>(),
                                     Replace<E1>.With<E2, E3>()));
   }

   [PCT]
   public async Task Replacing_E1_with_E2_E3()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E2, E3, Ef>(),
                                     Replace<E1>.With<E2, E3>()));
   }

   [PCT]
   public async Task Replacing_E1_with_E2_E3_2()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef, Ef, Ef, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E2, E3, Ef, Ef, Ef, Ef>(),
                                     Replace<E1>.With<E2, E3>()));
   }

   [PCT]
   public async Task Replacing_E1_with_E2_then_irrelevant_migration()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E2, Ef>(),
                                     Replace<E1>.With<E2>(),
                                     Replace<E1>.With<E5>()));
   }

   [PCT]
   public async Task Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E2, E3, Ef>(),
                                     Replace<E1>.With<E2, E3>(),
                                     Replace<E1>.With<E5>()));
   }

   [PCT]
   public async Task Replacing_E1_with_E2_E3_then_E2_with_E4()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E4, E3, Ef>(),
                                     Replace<E1>.With<E2, E3>(), //Ec1, E2, E3, Ef
                                     Replace<E2>.With<E4>()));   //Ec1, E4, E3, Ef
   }

   [PCT]
   public async Task Inserting_E3_before_E1()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E3, E1, Ef>(),
                                     Before<E1>.Insert<E3>()));
   }

   [PCT]
   public async Task Inserting_E3_E4_before_E1()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E3, E4, E1, Ef>(),
                                     Before<E1>.Insert<E3, E4>()));
   }

   [PCT]
   public async Task Inserting_E2_before_E1_then_E3_before_E2()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E3, E2, E1, Ef>(),
                                     Before<E1>.Insert<E2>(),
                                     Before<E2>.Insert<E3>()));
   }

   [PCT]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E3()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1>(),
                                     EnumerableCE.OfTypes<Ec1, E5, E3, E4, E1>(),
                                     Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1
                                     Before<E3>.Insert<E5>()));   //Ec1, E5, E3, E4, E1;
   }

   [PCT]
   public async Task Given_Ec1_E1_Ef_Inserting_E3_E4_before_E1_then_E5_before_E4()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E3, E5, E4, E1, Ef>(),
                                     Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1, Ef
                                     Before<E4>.Insert<E5>()));   //Ec1, E3, E5, E4, E1, Ef
   }

   [PCT]
   public async Task Given_Ec1_E1_Inserting_E2_before_E1_then_E3_before_E2()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1>(),
                                     EnumerableCE.OfTypes<Ec1, E3, E2, E1>(),
                                     Before<E1>.Insert<E2>(),   //Ec1, E2, E1
                                     Before<E2>.Insert<E3>())); //Ec1, E3, E2, E1
   }

   [PCT]
   public async Task Given_Ec1_E1_Inserting_E3_E2_before_E1_then_E4_before_E3_then_E5_before_E4()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1>(),
                                     EnumerableCE.OfTypes<Ec1, E5, E4, E3, E2, E1>(),
                                     Before<E1>.Insert<E3, E2>(), //Ec1, E3, E2, E1
                                     Before<E3>.Insert<E4>(),     //Ec1, E4, E3, E2, E1
                                     Before<E4>.Insert<E5>()));   //Ec1, E5, E4, E3, E2, E1
   }

   [PCT]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E6, E5, E4, E1, Ef>(),
                                     Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1, Ef
                                     Before<E4>.Insert<E5>(),     //Ec1, E3, E5, E4, E1, Ef
                                     Replace<E3>.With<E6>()));    //Ec1, E6, E5, E4, E1, Ef
   }

   [PCT]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6_then_replace_Ef_with_E7_then_insert_E8_after_E7()
   {
      await RunMigrationTestAsync(new MigrationScenario(EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                                        EnumerableCE.OfTypes<Ec1, E6, E5, E4, E1, E7, E8>(),
                                                        Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1, Ef
                                                        Before<E4>.Insert<E5>(),     //Ec1, E3, E5, E4, E1, Ef
                                                        Replace<E3>.With<E6>(),      //Ec1, E6, E5, E4, E1, Ef
                                                        Replace<Ef>.With<E7>(),      //Ec1, E6, E5, E4, E1, E7
                                                        After<E7>.Insert<E8>()));    //Ec1, E6, E5, E4, E1, E7, E8
   }

   [PCT]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E3_2()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E5, E3, E4, E1, Ef, Ef>(),
                                     Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4, E1, Ef, Ef
                                     Before<E3>.Insert<E5>()));   //Ec1, E5, E3, E4, E1, Ef, Ef
   }

   [PCT]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E4_2()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E3, E5, E4, E1, Ef, Ef>(),
                                     Before<E1>.Insert<E3, E4>(), //Ec1, E3, E4 E1, Ef, Ef
                                     Before<E4>.Insert<E5>()));   //Ec1, E3, E5, E4, E1, Ef, Ef
   }

   [PCT]
   public async Task Inserting_E2_after_E1()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                     EnumerableCE.OfTypes<Ec1, E1, E2, Ef, Ef>(),
                                     After<E1>.Insert<E2>()));
   }

   [PCT]
   public async Task Inserting_E2_after_E1_at_end_of_stream()
   {
      await RunMigrationTestAsync(new MigrationScenario(
                                     EnumerableCE.OfTypes<Ec1, E1>(),
                                     EnumerableCE.OfTypes<Ec1, E1, E2>(),
                                     After<E1>.Insert<E2>()));
   }

   [PCT]
   public async Task Given_Ec1_E1_before_E1_E2_after_E2_E3_throws_NonIdempotentMigrationDetectedException()
   {
      await this.InvokingAsync(_ =>
                                  RunMigrationTestAsync(
                                     new MigrationScenario(
                                        EnumerableCE.OfTypes<Ec1, E1>(),
                                        EnumerableCE.OfTypes<Ec1, E2, E3, E1>(),
                                        Before<E1>.Insert<E2>(),
                                        After<E2>.Insert<E3>())))
                .Must().ThrowAsync<NonIdempotentMigrationDetectedException>();
   }

   [PCT]
   public async Task PersistingMigrationsOfTheSameTaggregateMultipleTimes()
   {
      var emptyMigrationsArray = Array.Empty<ITeventMigration>();
      IReadOnlyList<ITeventMigration> migrations = emptyMigrationsArray;

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var container = CreateContainerForTeventStoreType(() => migrations);
         toDispose.Add(container);

         var id = new TaggregateId();

         UtcTimeSource.Test.FrozenAtUtcNow().Run(() =>
         {
            var taggregate = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E1, E2, E3, E4>());
            var initialHistory = taggregate.History;

            var firstSavedHistory = container.ExecuteUnitOfWork(unitOfWork =>
            {
               unitOfWork.TeventStoreUpdater().Save(taggregate);
               return unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History;
            });

            AssertStreamsAreIdenticalExceptForEventIds(initialHistory, firstSavedHistory, "first saved history");

            migrations = [Replace<E1>.With<E5>()];
            ClearCache(container);

            var migratedHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            var expectedAfterReplacingE1WithE5 =
               TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E5, E2, E3, E4>()).History;
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE1WithE5, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

            var historyAfterPersistingButBeforeReload = container.ExecuteInIsolatedScope(scope =>
            {
               scope.TeventStore().PersistMigrations();
               return TransactionScopeCe.Execute(() => scope.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            });

            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");

            var historyAfterPersistingAndReloading = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

            migrations = [Replace<E2>.With<E6>()];

            toDispose.Add(container = container.CloneAndBuild());

            migratedHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            var expectedAfterReplacingE2WithE6 = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E5, E6, E3, E4>()).History;
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE2WithE6, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

            historyAfterPersistingButBeforeReload = container.ExecuteInIsolatedScope(scope =>
            {
               scope.TeventStore().PersistMigrations();
               return TransactionScopeCe.Execute(() => scope.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            });

            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");
            historyAfterPersistingAndReloading = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");
         });
      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }
   }

   [PCT]
   public async Task PersistingMigrationsOfTheSameTaggregateMultipleTimesWithTeventsAddedInTheMiddleAndAfter()
   {
      var emptyMigrationsArray = Array.Empty<ITeventMigration>();
      IReadOnlyList<ITeventMigration> migrations = emptyMigrationsArray;

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var container = CreateContainerForTeventStoreType(() => migrations);
         toDispose.Add(container);

         var id = new TaggregateId();

         UtcTimeSource.Test.FrozenAtUtcNow().Run(() =>
         {
            var taggregate = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E1, E2, E3, E4>());
            var initialHistory = taggregate.History;

            var firstSavedHistory = container.ExecuteUnitOfWork(unitOfWork =>
            {
               unitOfWork.TeventStoreUpdater().Save(taggregate);
               return unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History;
            });

            AssertStreamsAreIdenticalExceptForEventIds(initialHistory, firstSavedHistory, "first saved history");

            migrations = [Replace<E1>.With<E5>()];
            ClearCache(container);

            var migratedHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            var expectedAfterReplacingE1WithE5 =
               TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E5, E2, E3, E4>()).History;
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE1WithE5, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

            var historyAfterPersistingButBeforeReload = container.ExecuteInIsolatedScope(scope =>
            {
               scope.TeventStore().PersistMigrations();
               return TransactionScopeCe.Execute(() => scope.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            });

            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");

            var historyAfterPersistingAndReloading = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

            container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).Publish(new E6(), new E7()));

            migrations = [Replace<E2>.With<E6>()];
            toDispose.Add(container = container.CloneAndBuild());

            migratedHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            var expectedAfterReplacingE2WithE6 = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E5, E6, E3, E4, E6, E7>()).History;
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE2WithE6, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

            historyAfterPersistingButBeforeReload = container.ExecuteInIsolatedScope(scope =>
            {
               scope.TeventStore().PersistMigrations();
               return TransactionScopeCe.Execute(() => scope.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            });

            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");
            historyAfterPersistingAndReloading = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

            migrations = Enumerable.Empty<ITeventMigration>().ToList();
            container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).Publish(new E8(), new E9()));
            historyAfterPersistingAndReloading = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            var expectedAfterReplacingE2WithE6AndRaisingE8E9 = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E5, E6, E3, E4, E6, E7, E8, E9>()).History;
            AssertStreamsAreIdenticalExceptForEventIds(expected: expectedAfterReplacingE2WithE6AndRaisingE8E9, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");
         });
      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }
   }

   [PCT]
   public async Task UpdatingAnTaggregateAfterPersistingMigrations()
   {
      IReadOnlyList<ITeventMigration> migrations = [];

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var container = CreateContainerForTeventStoreType(() => migrations);
         toDispose.Add(container);

         var id = new TaggregateId();

         UtcTimeSource.Test.FrozenAtUtcNow().Run(() =>
         {
            var initialTaggregate = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E1>());

            container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Save(initialTaggregate));

            migrations = [Replace<E1>.With<E5>()];

            container.ExecuteInIsolatedScope(scope => scope.TeventStore().PersistMigrations());

            migrations = EnumerableCE.Create<ITeventMigration>().ToList();

            container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).Publish(new E2()));

            var taggregate = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id));

            var expected = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E5, E2>()).History;
            AssertStreamsAreIdenticalExceptForEventIds(expected: expected, migratedHistory: taggregate.History, descriptionOfHistory: "migrated history");

            var completeTeventHistory = container.ExecuteInIsolatedScope(scope => scope.TeventStore().ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()).Tevents().ToList();
            AssertStreamsAreIdenticalExceptForEventIds(expected: expected, migratedHistory: completeTeventHistory, descriptionOfHistory: "streamed persisted history");

            toDispose.Add(container = container.CloneAndBuild());

            completeTeventHistory = container.ExecuteInIsolatedScope(scope => scope.TeventStore().ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()).Tevents().ToList();
            AssertStreamsAreIdenticalExceptForEventIds(expected: expected, migratedHistory: completeTeventHistory, descriptionOfHistory: "streamed persisted history");
         });
      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }
   }

   [PCT]
   public async Task Inserting_E2_Before_E1_Persisting_and_then_Inserting_E3_before_E1()
   {
      var firstMigration = EnumerableCE.Create(Before<E1>.Insert<E2>()).ToArray();
      var secondMigration = EnumerableCE.Create(Before<E1>.Insert<E3>()).ToArray();
      IReadOnlyList<ITeventMigration> migrations = new List<ITeventMigration>();

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var container = CreateContainerForTeventStoreType(() => migrations);
         toDispose.Add(container);
         UtcTimeSource.Test.FrozenAtUtc("2001-01-01 12:00").Run(() =>
         {
            var id = new TaggregateId();
            var initialTaggregate = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E1>());
            var expectedHistoryAfterFirstMigration = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E2, E1>()).History;
            var expectedHistoryAfterSecondMigration = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E2, E3, E1>()).History;

            container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Save(initialTaggregate));
            migrations = firstMigration;
            ClearCache(container);
            var historyWithFirstMigrationUnPersisted = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);

            container.ExecuteInIsolatedScope(scope => scope.TeventStore().PersistMigrations());
            var historyAfterPersistingFirstMigration = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            AssertStreamsAreIdenticalExceptForEventIds(expectedHistoryAfterFirstMigration, historyWithFirstMigrationUnPersisted, nameof(historyWithFirstMigrationUnPersisted));
            AssertStreamsAreIdenticalExceptForEventIds(expectedHistoryAfterFirstMigration, historyAfterPersistingFirstMigration, nameof(historyAfterPersistingFirstMigration));

            migrations = secondMigration;
            toDispose.Add(container = container.CloneAndBuild());
            var historyWithSecondMigrationUnPersisted = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);

            container.ExecuteInIsolatedScope(scope => scope.TeventStore().PersistMigrations());
            var historyAfterPersistingSecondMigration = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            AssertStreamsAreIdenticalExceptForEventIds(expectedHistoryAfterSecondMigration, historyWithSecondMigrationUnPersisted, nameof(historyWithSecondMigrationUnPersisted));
            AssertStreamsAreIdenticalExceptForEventIds(expectedHistoryAfterSecondMigration, historyAfterPersistingSecondMigration, nameof(historyAfterPersistingSecondMigration));
         });
      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }
   }

   [PCT]
   public async Task Inserting_E2_After_E1_Persisting_and_then_Inserting_E3_after_E1()
   {
      var firstMigration = EnumerableCE.Create(After<E1>.Insert<E2>()).ToArray();
      var secondMigration = EnumerableCE.Create(After<E1>.Insert<E3>()).ToArray();
      IReadOnlyList<ITeventMigration> migrations = new List<ITeventMigration>();

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var container = CreateContainerForTeventStoreType(() => migrations);
         toDispose.Add(container);
         UtcTimeSource.Test.FrozenAtUtc("2001-01-01 12:00").Run(() =>
         {
            var id = new TaggregateId();
            var initialTaggregate = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E1>());
            var expectedHistoryAfterFirstMigration = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E1, E2>()).History;
            var expectedHistoryAfterSecondMigration = TestTaggregate.FromTevents(id, EnumerableCE.OfTypes<Ec1, E1, E3, E2>()).History;

            container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Save(initialTaggregate));
            migrations = firstMigration;
            ClearCache(container);
            var historyWithFirstMigrationUnPersisted = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);

            container.ExecuteInIsolatedScope(scope => scope.TeventStore().PersistMigrations());
            var historyAfterPersistingFirstMigration = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            AssertStreamsAreIdenticalExceptForEventIds(expectedHistoryAfterFirstMigration, historyWithFirstMigrationUnPersisted, nameof(historyWithFirstMigrationUnPersisted));
            AssertStreamsAreIdenticalExceptForEventIds(expectedHistoryAfterFirstMigration, historyAfterPersistingFirstMigration, nameof(historyAfterPersistingFirstMigration));

            migrations = secondMigration;
            toDispose.Add(container = container.CloneAndBuild());
            var historyWithSecondMigrationUnPersisted = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);

            container.ExecuteInIsolatedScope(scope => scope.TeventStore().PersistMigrations());
            var historyAfterPersistingSecondMigration = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).History);
            AssertStreamsAreIdenticalExceptForEventIds(expectedHistoryAfterSecondMigration, historyWithSecondMigrationUnPersisted, nameof(historyWithSecondMigrationUnPersisted));
            AssertStreamsAreIdenticalExceptForEventIds(expectedHistoryAfterSecondMigration, historyAfterPersistingSecondMigration, nameof(historyAfterPersistingSecondMigration));
         });
      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }
   }

   [PCT]
   public async Task Inserting_E2_Before_E1()
   {
      await RunMigrationTestAsync(
         new MigrationScenario(
            EnumerableCE.OfTypes<Ec1, E1>(),
            EnumerableCE.OfTypes<Ec1, E2, E1>(),
            Before<E1>.Insert<E2>()));
   }

   [PCT]
   public async Task Persisting_migrations_and_then_updating_the_taggregate_from_another_processes_TeventStore_results_in_both_processes_seeing_identical_histories()
   {
      var actualMigrations = EnumerableCE.Create(Replace<E1>.With<E2>()).ToArray();
      IReadOnlyList<ITeventMigration> migrations = new List<ITeventMigration>();

      // ReSharper disable once AccessToModifiedClosure this is exactly what we wish to achieve here...
      var container = CreateContainerForTeventStoreType(() => migrations);
      await using var locator = container;

      var otherProcessContainer = container.CloneAndBuild();
      await using var processContainer = otherProcessContainer;

      var id = new TaggregateId();

      var taggregate = TestTaggregate.FromTevents(
         id,
         EnumerableCE.OfTypes<Ec1, E1, E2, E3, E4>());

      otherProcessContainer.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Save(taggregate));
      migrations = actualMigrations;
      otherProcessContainer.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id));

      var test = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStore().GetTaggregateHistory(id));
      test.Count.Must().BeGreaterThan(0);

      container.ExecuteInIsolatedScope(scope => scope.TeventStore().PersistMigrations());

      otherProcessContainer.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater().Get<TestTaggregate>(id).Publish(new E3()));

      var firstProcessHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStore().GetTaggregateHistory(id).Tevents().ToList());
      var secondProcessHistory = otherProcessContainer.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStore().GetTaggregateHistory(id).Tevents().ToList());

      TeventStorageTestHelper.StripSteventhDecimalPointFromSecondFractionOnUtcUpdateTime(firstProcessHistory);
      TeventStorageTestHelper.StripSteventhDecimalPointFromSecondFractionOnUtcUpdateTime(secondProcessHistory);

      AssertStreamsAreIdenticalExceptForEventIds(firstProcessHistory, secondProcessHistory, "Both process histories should be identical");
   }
}


