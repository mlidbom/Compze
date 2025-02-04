﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.GenericAbstractions.Time;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Refactoring.Migrations;
using Compze.SystemCE.LinqCE;
using Compze.Testing.DependencyInjection;
using Compze.Testing.SystemCE.TransactionsCE;
using Compze.Tests.CQRS.EventRefactoring.Migrations;
using Compze.Tests.CQRS.EventRefactoring.Migrations.Events;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable AccessToDisposedClosure
// ReSharper disable AccessToModifiedClosure

namespace Compze.Tests.Integration.CQRS.EventRefactoring.Migrations;

//Todo: Write tests that verify that none of the persistence layers lose precision in the persisted ReadOrder when persisting refactorings.
public class EventMigrationTest(string pluggableComponentsCombination) : EventMigrationTestBase(pluggableComponentsCombination)
{
   [Test]
   public async Task Base_class_method_should_detect_incorrect_type_order()
   {
      await this.Invoking(
                    _ => RunMigrationTest(
                       new MigrationScenario(
                          EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                          EnumerableCE.OfTypes<Ec1, Ef, E2, Ef>())))
                .Should().ThrowAsync<Exception>();
   }

   [Test]
   public async Task Replacing_E1_with_E2()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E2, Ef, Ef>(),
                                Replace<E1>.With<E2>()));
   }

   [Test]
   public async Task Replacing_E1_with_E2_at_end_of_stream()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1>(),
                                EnumerableCE.OfTypes<Ec1, E2>(),
                                Replace<E1>.With<E2>()));
   }

   [Test]
   public async Task Replacing_E1_with_E2_E3_at_end_of_stream()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1>(),
                                EnumerableCE.OfTypes<Ec1, E2, E3>(),
                                Replace<E1>.With<E2, E3>()));
   }

   [Test]
   public async Task Replacing_E1_with_E2_E3()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E2, E3, Ef>(),
                                Replace<E1>.With<E2, E3>()));
   }

   [Test]
   public async Task Replacing_E1_with_E2_E3_2()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef, Ef, Ef, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E2, E3, Ef, Ef, Ef, Ef>(),
                                Replace<E1>.With<E2, E3>()));
   }

   [Test]
   public async Task Replacing_E1_with_E2_then_irrelevant_migration()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E2, Ef>(),
                                Replace<E1>.With<E2>(),
                                Replace<E1>.With<E5>()));
   }

   [Test]
   public async Task Replacing_E1_with_E2_E3_then_an_unrelated_migration_v2()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E2, E3, Ef>(),
                                Replace<E1>.With<E2, E3>(),
                                Replace<E1>.With<E5>()));
   }

   [Test]
   public async Task Replacing_E1_with_E2_E3_then_E2_with_E4()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E4, E3, Ef>(),
                                Replace<E1>.With<E2, E3>(),                                                //Ec1, E2, E3, Ef
                                Replace<E2>.With<E4>())); //Ec1, E4, E3, Ef
   }

   [Test]
   public async Task Inserting_E3_before_E1()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E3, E1, Ef>(),
                                Before<E1>.Insert<E3>()));
   }

   [Test]
   public async Task Inserting_E3_E4_before_E1()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E3, E4, E1, Ef>(),
                                Before<E1>.Insert<E3, E4>()));
   }

   [Test]
   public async Task Inserting_E2_before_E1_then_E3_before_E2()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E3, E2, E1, Ef>(),
                                Before<E1>.Insert<E2>(),
                                Before<E2>.Insert<E3>()));
   }

   [Test]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E3()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1>(),
                                EnumerableCE.OfTypes<Ec1, E5, E3, E4, E1>(),
                                Before<E1>.Insert<E3, E4>(),                                                //Ec1, E3, E4, E1
                                Before<E3>.Insert<E5>())); //Ec1, E5, E3, E4, E1;
   }

   [Test]
   public async Task Given_Ec1_E1_Ef_Inserting_E3_E4_before_E1_then_E5_before_E4()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E3, E5, E4, E1, Ef>(),
                                Before<E1>.Insert<E3, E4>(),                                                //Ec1, E3, E4, E1, Ef
                                Before<E4>.Insert<E5>())); //Ec1, E3, E5, E4, E1, Ef
   }

   [Test]
   public async Task Given_Ec1_E1_Inserting_E2_before_E1_then_E3_before_E2()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1>(),
                                EnumerableCE.OfTypes<Ec1, E3, E2, E1>(),
                                Before<E1>.Insert<E2>(),                                                    //Ec1, E2, E1
                                Before<E2>.Insert<E3>())); //Ec1, E3, E2, E1
   }

   [Test]
   public async Task Given_Ec1_E1_Inserting_E3_E2_before_E1_then_E4_before_E3_then_E5_before_E4()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1>(),
                                EnumerableCE.OfTypes<Ec1, E5, E4, E3, E2, E1>(),
                                Before<E1>.Insert<E3, E2>(),                                                //Ec1, E3, E2, E1
                                Before<E3>.Insert<E4>(),                                                    //Ec1, E4, E3, E2, E1
                                Before<E4>.Insert<E5>())); //Ec1, E5, E4, E3, E2, E1
   }

   [Test]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E6, E5, E4, E1, Ef>(),
                                Before<E1>.Insert<E3, E4>(),                                               //Ec1, E3, E4, E1, Ef
                                Before<E4>.Insert<E5>(),                                                   //Ec1, E3, E5, E4, E1, Ef
                                Replace<E3>.With<E6>())); //Ec1, E6, E5, E4, E1, Ef
   }

   [Test]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E4_then_replace_E4_with_E6_then_replace_Ef_with_E7_then_insert_E8_after_E7()
   {
      await RunMigrationTest(new MigrationScenario
                             (EnumerableCE.OfTypes<Ec1, E1, Ef>(),
                              EnumerableCE.OfTypes<Ec1, E6, E5, E4, E1, E7, E8>(),
                              Before<E1>.Insert<E3, E4>(),                                               //Ec1, E3, E4, E1, Ef
                              Before<E4>.Insert<E5>(),                                                   //Ec1, E3, E5, E4, E1, Ef
                              Replace<E3>.With<E6>(),                                                    //Ec1, E6, E5, E4, E1, Ef
                              Replace<Ef>.With<E7>(),                                                    //Ec1, E6, E5, E4, E1, E7
                              After<E7>.Insert<E8>())); //Ec1, E6, E5, E4, E1, E7, E8
   }

   [Test]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E3_2()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E5, E3, E4, E1, Ef, Ef>(),
                                Before<E1>.Insert<E3, E4>(),                                                //Ec1, E3, E4, E1, Ef, Ef
                                Before<E3>.Insert<E5>())); //Ec1, E5, E3, E4, E1, Ef, Ef
   }

   [Test]
   public async Task Inserting_E3_E4_before_E1_then_E5_before_E4_2()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E3, E5, E4, E1, Ef, Ef>(),
                                Before<E1>.Insert<E3, E4>(),                                                //Ec1, E3, E4 E1, Ef, Ef
                                Before<E4>.Insert<E5>())); //Ec1, E3, E5, E4, E1, Ef, Ef
   }

   [Test]
   public async Task Inserting_E2_after_E1()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1, Ef, Ef>(),
                                EnumerableCE.OfTypes<Ec1, E1, E2, Ef, Ef>(),
                                After<E1>.Insert<E2>()));

   }

   [Test]
   public async Task Inserting_E2_after_E1_at_end_of_stream()
   {
      await RunMigrationTest(new MigrationScenario(
                                EnumerableCE.OfTypes<Ec1, E1>(),
                                EnumerableCE.OfTypes<Ec1, E1, E2>(),
                                After<E1>.Insert<E2>()));
   }

   [Test]
   public async Task Given_Ec1_E1_before_E1_E2_after_E2_E3_throws_NonIdempotentMigrationDetectedException()
   {
      await this.Invoking(
                    _ =>
                       RunMigrationTest(
                          new MigrationScenario(
                             EnumerableCE.OfTypes<Ec1, E1>(),
                             EnumerableCE.OfTypes<Ec1, E2, E3, E1>(),
                             Before<E1>.Insert<E2>(),
                             After<E2>.Insert<E3>())))
                .Should().ThrowAsync<NonIdempotentMigrationDetectedException>();
   }

   [Test]
   public async Task PersistingMigrationsOfTheSameAggregateMultipleTimes()
   {
      var emptyMigrationsArray = Array.Empty<IEventMigration>();
      IReadOnlyList<IEventMigration> migrations = emptyMigrationsArray;

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations);
         toDispose.Add(serviceLocator);

         var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

         serviceLocator.Resolve<TestingTimeSource>().FreezeAtUtcTime("2001-01-01 12:00");

         var aggregate = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E1, E2, E3, E4>());
         var initialHistory = aggregate.History;

         IEventStoreUpdater Session() => serviceLocator.Resolve<IEventStoreUpdater>();
         IEventStore EventStore() => serviceLocator.Resolve<IEventStore>();

         var firstSavedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(
            () =>
            {
               Session().Save(aggregate);
               return Session().Get<TestAggregate>(id).History;
            });


         AssertStreamsAreIdentical(initialHistory, firstSavedHistory, "first saved history");

         migrations = [Replace<E1>.With<E5>()];
         ClearCache(serviceLocator);

         var migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         var expectedAfterReplacingE1WithE5 =
            TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E5, E2, E3, E4>()).History;
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

         var historyAfterPersistingButBeforeReload = serviceLocator.ExecuteInIsolatedScope(
            () =>
            {
               EventStore().PersistMigrations();
               return TransactionScopeCe.Execute(() => Session().Get<TestAggregate>(id).History);
            });

         AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");

         var historyAfterPersistingAndReloading = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

         migrations = [Replace<E2>.With<E6>()];

         toDispose.Add(serviceLocator = serviceLocator.Clone());

         migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         var expectedAfterReplacingE2WithE6 = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E5, E6, E3, E4>()).History;
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

         historyAfterPersistingButBeforeReload = serviceLocator.ExecuteInIsolatedScope(
            () =>
            {
               EventStore().PersistMigrations();
               return TransactionScopeCe.Execute(() => Session().Get<TestAggregate>(id).History);
            });

         AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");
         historyAfterPersistingAndReloading = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");
      }finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }

   }


   [Test]
   public async Task PersistingMigrationsOfTheSameAggregateMultipleTimesWithEventsAddedInTheMiddleAndAfter()
   {
      var emptyMigrationsArray = Array.Empty<IEventMigration>();
      IReadOnlyList<IEventMigration> migrations = emptyMigrationsArray;

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations);
         toDispose.Add(serviceLocator);

         var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

         serviceLocator.Resolve<TestingTimeSource>().FreezeAtUtcTime("2001-01-01 12:00");

         var aggregate = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E1, E2, E3, E4>());
         var initialHistory = aggregate.History;

         IEventStoreUpdater Session() => serviceLocator.Resolve<IEventStoreUpdater>();
         IEventStore EventStore() => serviceLocator.Resolve<IEventStore>();

         var firstSavedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(
            () =>
            {
               Session().Save(aggregate);
               return Session().Get<TestAggregate>(id).History;
            });


         AssertStreamsAreIdentical(initialHistory, firstSavedHistory, "first saved history");

         migrations = [Replace<E1>.With<E5>()];
         ClearCache(serviceLocator);

         var migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         var expectedAfterReplacingE1WithE5 =
            TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E5, E2, E3, E4>()).History;
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

         var historyAfterPersistingButBeforeReload = serviceLocator.ExecuteInIsolatedScope(
            () =>
            {
               EventStore().PersistMigrations();
               return TransactionScopeCe.Execute(() => Session().Get<TestAggregate>(id).History);
            });

         AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");

         var historyAfterPersistingAndReloading = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE1WithE5, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

         serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).Publish(new E6(), new E7()));

         migrations = [Replace<E2>.With<E6>()];
         toDispose.Add(serviceLocator = serviceLocator.Clone());

         migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         var expectedAfterReplacingE2WithE6 = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E5, E6, E3, E4, E6, E7>()).History;
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: migratedHistory, descriptionOfHistory: "migrated history");

         historyAfterPersistingButBeforeReload = serviceLocator.ExecuteInIsolatedScope(
            () =>
            {
               EventStore().PersistMigrations();
               return TransactionScopeCe.Execute(() => Session().Get<TestAggregate>(id).History);
            });

         AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingButBeforeReload, descriptionOfHistory: "migrated, persisted");
         historyAfterPersistingAndReloading = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

         migrations = Enumerable.Empty<IEventMigration>().ToList();
         serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).Publish(new E8(), new E9()));
         historyAfterPersistingAndReloading = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         var expectedAfterReplacingE2WithE6AndRaisingE8E9 = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E5, E6, E3, E4, E6, E7, E8, E9>()).History;
         AssertStreamsAreIdentical(expected: expectedAfterReplacingE2WithE6AndRaisingE8E9, migratedHistory: historyAfterPersistingAndReloading, descriptionOfHistory: "migrated, persisted, reloaded");

      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }

   }

   [Test]
   public async Task UpdatingAnAggregateAfterPersistingMigrations()
   {
      IReadOnlyList<IEventMigration> migrations = Array.Empty<IEventMigration>();

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations);
         toDispose.Add(serviceLocator);

         var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

         serviceLocator.Resolve<TestingTimeSource>().FreezeAtUtcTime("2001-01-01 12:00");

         var initialAggregate = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E1>());

         IEventStoreUpdater Session() => serviceLocator.Resolve<IEventStoreUpdater>();
         IEventStore EventStore() => serviceLocator.Resolve<IEventStore>();

         serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Save(initialAggregate));

         migrations = [Replace<E1>.With<E5>()];

         serviceLocator.ExecuteInIsolatedScope(() => EventStore().PersistMigrations());

         migrations = EnumerableCE.Create<IEventMigration>().ToList();

         serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).Publish(new E2()));

         var aggregate = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id));

         var expected = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E5, E2>()).History;
         AssertStreamsAreIdentical(expected: expected, migratedHistory: aggregate.History, descriptionOfHistory: "migrated history");

         var completeEventHistory =serviceLocator.ExecuteInIsolatedScope(() => EventStore().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()).Cast<AggregateEvent>().ToList();
         AssertStreamsAreIdentical(expected: expected, migratedHistory: completeEventHistory, descriptionOfHistory: "streamed persisted history");

         toDispose.Add(serviceLocator = serviceLocator.Clone());

         completeEventHistory = serviceLocator.ExecuteInIsolatedScope(() => EventStore().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()).Cast<AggregateEvent>().ToList();
         AssertStreamsAreIdentical(expected: expected, migratedHistory: completeEventHistory, descriptionOfHistory: "streamed persisted history");
      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }
   }

   [Test]
   public async Task Inserting_E2_Before_E1_Persisting_and_then_Inserting_E3_before_E1()
   {
      var firstMigration = EnumerableCE.Create(Before<E1>.Insert<E2>()).ToArray();
      var secondMigration = EnumerableCE.Create(Before<E1>.Insert<E3>()).ToArray();
      IReadOnlyList<IEventMigration> migrations = new List<IEventMigration>();

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations);
         toDispose.Add(serviceLocator);
         serviceLocator.Resolve<TestingTimeSource>().FreezeAtUtcTime("2001-01-01 12:00");

         var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
         var initialAggregate = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E1>());
         var expectedHistoryAfterFirstMigration = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E2, E1>()).History;
         var expectedHistoryAfterSecondMigration = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E2, E3, E1>()).History;

         IEventStoreUpdater Session() => serviceLocator.Resolve<IEventStoreUpdater>();
         IEventStore EventStore() => serviceLocator.Resolve<IEventStore>();

         serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Save(initialAggregate));
         migrations = firstMigration;
         ClearCache(serviceLocator);
         var historyWithFirstMigrationUnPersisted = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);

         serviceLocator.ExecuteInIsolatedScope(() => EventStore().PersistMigrations());
         var historyAfterPersistingFirstMigration = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         AssertStreamsAreIdentical(expectedHistoryAfterFirstMigration, historyWithFirstMigrationUnPersisted, nameof(historyWithFirstMigrationUnPersisted));
         AssertStreamsAreIdentical(expectedHistoryAfterFirstMigration, historyAfterPersistingFirstMigration, nameof(historyAfterPersistingFirstMigration));

         migrations = secondMigration;
         toDispose.Add(serviceLocator = serviceLocator.Clone());
         var historyWithSecondMigrationUnPersisted = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);

         serviceLocator.ExecuteInIsolatedScope(() => EventStore().PersistMigrations());
         var historyAfterPersistingSecondMigration = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         AssertStreamsAreIdentical(expectedHistoryAfterSecondMigration, historyWithSecondMigrationUnPersisted, nameof(historyWithSecondMigrationUnPersisted));
         AssertStreamsAreIdentical(expectedHistoryAfterSecondMigration, historyAfterPersistingSecondMigration, nameof(historyAfterPersistingSecondMigration));
      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }
   }

   [Test]
   public async Task Inserting_E2_After_E1_Persisting_and_then_Inserting_E3_after_E1()
   {
      var firstMigration = EnumerableCE.Create(After<E1>.Insert<E2>()).ToArray();
      var secondMigration = EnumerableCE.Create(After<E1>.Insert<E3>()).ToArray();
      IReadOnlyList<IEventMigration> migrations = new List<IEventMigration>();

      List<IAsyncDisposable> toDispose = [];
      try
      {
         var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations);
         toDispose.Add(serviceLocator);
         serviceLocator.Resolve<TestingTimeSource>().FreezeAtUtcTime("2001-01-01 12:00");

         var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
         var initialAggregate = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E1>());
         var expectedHistoryAfterFirstMigration = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E1, E2>()).History;
         var expectedHistoryAfterSecondMigration = TestAggregate.FromEvents(serviceLocator.Resolve<IUtcTimeTimeSource>(), id, EnumerableCE.OfTypes<Ec1, E1, E3, E2>()).History;

         IEventStoreUpdater Session() => serviceLocator.Resolve<IEventStoreUpdater>();
         IEventStore EventStore() => serviceLocator.Resolve<IEventStore>();

         serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Save(initialAggregate));
         migrations = firstMigration;
         ClearCache(serviceLocator);
         var historyWithFirstMigrationUnPersisted = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);

         serviceLocator.ExecuteInIsolatedScope(() => EventStore().PersistMigrations());
         var historyAfterPersistingFirstMigration = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         AssertStreamsAreIdentical(expectedHistoryAfterFirstMigration, historyWithFirstMigrationUnPersisted, nameof(historyWithFirstMigrationUnPersisted));
         AssertStreamsAreIdentical(expectedHistoryAfterFirstMigration, historyAfterPersistingFirstMigration, nameof(historyAfterPersistingFirstMigration));

         migrations = secondMigration;
         toDispose.Add(serviceLocator = serviceLocator.Clone());
         var historyWithSecondMigrationUnPersisted = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);

         serviceLocator.ExecuteInIsolatedScope(() => EventStore().PersistMigrations());
         var historyAfterPersistingSecondMigration = serviceLocator.ExecuteTransactionInIsolatedScope(() => Session().Get<TestAggregate>(id).History);
         AssertStreamsAreIdentical(expectedHistoryAfterSecondMigration, historyWithSecondMigrationUnPersisted, nameof(historyWithSecondMigrationUnPersisted));
         AssertStreamsAreIdentical(expectedHistoryAfterSecondMigration, historyAfterPersistingSecondMigration, nameof(historyAfterPersistingSecondMigration));
      }
      finally
      {
         await Task.WhenAll(toDispose.Select(it => it.DisposeAsync().AsTask()));
      }
   }

   [Test]
   public async Task Inserting_E2_Before_E1()
   {
      await RunMigrationTest(
         new MigrationScenario(
            EnumerableCE.OfTypes<Ec1, E1>(),
            EnumerableCE.OfTypes<Ec1, E2, E1>(),
            Before<E1>.Insert<E2>()));
   }

   [Test]
   public async Task Persisting_migrations_and_then_updating_the_aggregate_from_another_processes_EventStore_results_in_both_processes_seeing_identical_histories()
   {
      var actualMigrations = EnumerableCE.Create(Replace<E1>.With<E2>()).ToArray();
      IReadOnlyList<IEventMigration> migrations = new List<IEventMigration>();

      // ReSharper disable once AccessToModifiedClosure this is exactly what we wish to achieve here...
      var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations);
      await using var locator = serviceLocator;

      var otherProcessServiceLocator = serviceLocator.Clone();
      await using var processServiceLocator = otherProcessServiceLocator;

      var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

      var aggregate = TestAggregate.FromEvents(
         serviceLocator.Resolve<IUtcTimeTimeSource>(),
         id,
         EnumerableCE.OfTypes<Ec1, E1, E2, E3, E4>());

      otherProcessServiceLocator.ExecuteTransactionInIsolatedScope(() => OtherEventStoreSession().Save(aggregate));
      migrations = actualMigrations;
      otherProcessServiceLocator.ExecuteTransactionInIsolatedScope(() => OtherEventStoreSession().Get<TestAggregate>(id));

      var test = serviceLocator.ExecuteTransactionInIsolatedScope(() => PersistingEventStore().GetAggregateHistory(id));
      test.Count.Should().BeGreaterThan(0);

      serviceLocator.ExecuteInIsolatedScope(() => PersistingEventStore().PersistMigrations());

      otherProcessServiceLocator.ExecuteTransactionInIsolatedScope(() => OtherEventStoreSession().Get<TestAggregate>(id).Publish(new E3()));

      var firstProcessHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => PersistingEventStore().GetAggregateHistory(id));
      var secondProcessHistory = otherProcessServiceLocator.ExecuteTransactionInIsolatedScope(() => otherProcessServiceLocator.Resolve<IEventStore>().GetAggregateHistory(id));

      EventStorageTestHelper.StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(firstProcessHistory);
      EventStorageTestHelper.StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(secondProcessHistory);

      EventMigrationTestBase.AssertStreamsAreIdentical(firstProcessHistory, secondProcessHistory, "Both process histories should be identical");
      return;

      IEventStore PersistingEventStore() => serviceLocator.Resolve<IEventStore>();

      // ReSharper disable once AccessToDisposedClosure
      IEventStoreUpdater OtherEventStoreSession() => otherProcessServiceLocator.Resolve<IEventStoreUpdater>();
   }
}