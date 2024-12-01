using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Testing;
using Compze.GenericAbstractions.Time;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Refactoring.Migrations;
using Compze.SystemCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing.Performance;
using Compze.Tests.CQRS.EventRefactoring.Migrations.Events;
using NUnit.Framework;
using Compze.Testing;
using JetBrains.Annotations;

namespace Compze.Tests.CQRS.EventRefactoring.Migrations;

[LongRunning]
public class EventMigrationPerformanceTest([NotNull] string pluggableComponentsCombination) : EventMigrationTestBase(pluggableComponentsCombination)
{
   List<AggregateEvent> _history;
   TestAggregate _aggregate;
   IServiceLocator _container;
   IReadOnlyList<IEventMigration> _currentMigrations;
   [OneTimeSetUp] public void Given_a_1000_events_large_aggregate()
   {
      var historyTypes = EnumerableCE.OfTypes<Ec1>()
                                     .Concat(
                                         1.Through(10)
                                          .SelectMany(
                                              _ => 1.Through(96)
                                                    .Select(_ => typeof(E1))
                                                    .Concat(EnumerableCE.OfTypes<E2, E4, E6, E8>()))).ToList();

      _aggregate = TestAggregate.FromEvents(TestingTimeSource.FrozenUtcNow(), Guid.NewGuid(), historyTypes);
      _history = _aggregate.History.Cast<AggregateEvent>().ToList();

      _currentMigrations = Enumerable.Empty<IEventMigration>().ToList();
      _container = CreateServiceLocatorForEventStoreType(migrationsfactory: () => _currentMigrations);

      _container.ExecuteTransactionInIsolatedScope(() => _container.Resolve<IEventStore>().SaveSingleAggregateEvents(_history));
   }

   [OneTimeTearDown] public async Task TearDownTask()
   {
      if(_container != null)
         await _container.DisposeAsync().CaF();
   }

   async Task AssertUncachedAndCachedAggregateLoadTimes(TimeSpan maxUncachedLoadTime, TimeSpan maxCachedLoadTime, IReadOnlyList<IEventMigration> migrations)
   {
      _currentMigrations = migrations;

      IServiceLocator clonedLocator = null;

      await TimeAsserter.ExecuteAsync(
         description: "Uncached loading",
         maxTotal: maxUncachedLoadTime,
         setup: () => clonedLocator = _container.Clone(),
         tearDownAsync: async Task () => await clonedLocator.NotNull().DisposeAsync().CaF(),
         action: () =>
         {
            LoadWithCloneLocator(clonedLocator!);
            return Task.CompletedTask;
         }).CaF();

      await using((clonedLocator = _container.Clone()).CaF())
      {
         LoadWithCloneLocator(clonedLocator); //Warm up cache

         TimeAsserter.Execute(
            description: "Cached loading",
            maxTotal: maxCachedLoadTime,
            action: () => LoadWithCloneLocator(clonedLocator));
      }

      return;

      void LoadWithCloneLocator(IServiceLocator locator) => locator.ExecuteTransactionInIsolatedScope(() => locator.Resolve<IEventStoreUpdater>()
                                                                                                                   .Get<TestAggregate>(_aggregate.Id));
   }

   //Performance: Figure out why oracle under performs so dramatically in these tests and fix it. (Hmm. Adding FOR UPDATE to the DB2 query really really slowed DB2 down. Might Oracle be similar?)
   [Test] public async Task With_four_migrations_mutation_that_all_actually_changes_things_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_Y_milliseconds_mSSql_25_5_pgSql_25_5_mySql_25_5_orcl_125_5_inMem_15_DB2_30_5()
   {
      var eventMigrations = EnumerableCE.Create<IEventMigration>(
         Before<E2>.Insert<E3>(),
         Before<E4>.Insert<E5>(),
         Before<E6>.Insert<E7>(),
         Before<E8>.Insert<E9>()
      ).ToArray();

      await AssertUncachedAndCachedAggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.PersistenceLayer.ValueFor(db2: 30, memory: 15, msSql: 25, mySql: 55, orcl: 125, pgSql: 25).Milliseconds().EnvMultiply(instrumented: 2.5),
         maxCachedLoadTime: TestEnv.PersistenceLayer.ValueFor(db2: 5, memory: 5, msSql: 5, mySql: 5, orcl: 5, pgSql: 5).Milliseconds().EnvMultiply(instrumented: 2.5),
         eventMigrations).CaF();
   }

   [Test] public async Task With_four_migrations_that_change_nothing_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_X_milliseconds_mSSql_30_5_pgSql_30_5_mySql_30_5_orcl_120_5_inMem_15_DB2_30_5()
   {
      var eventMigrations = EnumerableCE.Create<IEventMigration>(
         Before<E3>.Insert<E1>(),
         Before<E5>.Insert<E1>(),
         Before<E7>.Insert<E1>(),
         Before<E9>.Insert<E1>()
      ).ToArray();

      await AssertUncachedAndCachedAggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.PersistenceLayer.ValueFor(db2: 30, memory: 15, msSql: 30, mySql: 30, orcl: 120, pgSql: 30).Milliseconds().EnvMultiply(instrumented: 2.5),
         maxCachedLoadTime: TestEnv.PersistenceLayer.ValueFor(db2: 5, memory: 5, msSql: 5, mySql: 5, orcl: 5, pgSql: 5).Milliseconds().EnvMultiply(instrumented: 2),
         eventMigrations).CaF();
   }

   [Test] public async Task When_there_are_no_migrations_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_Y_milliseconds_mSSql_20_5_pgSql_20_5_mySql_20_5_orcl_125_5_inMem_10_DB2_30_5()
   {
      var eventMigrations = EnumerableCE.Create<IEventMigration>().ToArray();
      await AssertUncachedAndCachedAggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.PersistenceLayer.ValueFor(db2: 30, memory: 10, msSql: 20, mySql: 45, orcl: 125, pgSql: 20).Milliseconds().EnvMultiply(instrumented: 3),
         maxCachedLoadTime: TestEnv.PersistenceLayer.ValueFor(db2: 5, memory: 5, msSql: 5, mySql: 5, orcl: 5, pgSql: 5).Milliseconds().EnvMultiply(instrumented: 2.5),
         eventMigrations).CaF();
   }
}