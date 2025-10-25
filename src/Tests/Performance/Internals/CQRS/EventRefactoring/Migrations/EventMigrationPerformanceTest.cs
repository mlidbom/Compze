using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time.Testing.Public;
using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Tests.Common.CQRS.EventRefactoring.Migrations;
using Compze.Tests.Common.CQRS.EventRefactoring.Migrations.Events;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.CQRS.EventRefactoring.Migrations;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tests.Performance.Internals.CQRS.EventRefactoring.Migrations;

[LongRunning]
public class EventMigrationPerformanceTest : EventMigrationTestBase
{
   readonly List<AggregateEvent> _history;
   readonly TestAggregate _aggregate;
   readonly IServiceLocator? _container;
   IReadOnlyList<IEventMigration> _currentMigrations;
   public EventMigrationPerformanceTest()
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
      _container = CreateServiceLocatorForEventStoreType(migrationsFactory: () => _currentMigrations);

      _container.ExecuteTransactionInIsolatedScope(() => _container.Resolve<IEventStore>().SaveSingleAggregateEvents(_history));
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null)
         await _container.DisposeAsync();
   }

   async Task AssertUncachedAndCachedAggregateLoadTimes(TimeSpan maxUncachedLoadTime, TimeSpan maxCachedLoadTime, IReadOnlyList<IEventMigration> migrations)
   {
      _currentMigrations = migrations;

      IServiceLocator? clonedLocator = null;

      await TimeAsserter.ExecuteAsync(
         description: "Uncached loading",
         maxTotal: maxUncachedLoadTime,
         setup: () => clonedLocator = _container!.Clone(),
         tearDownAsync: async Task () => await clonedLocator.NotNull().DisposeAsync(),
         action: () =>
         {
            LoadWithCloneLocator(clonedLocator!);
            return Task.CompletedTask;
         });

      await using(clonedLocator = _container!.Clone())
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
   [PCT]  public async Task With_four_migrations_mutation_that_all_actually_changes_things_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_Y_milliseconds_mSSql_25_5_pgSql_25_5_mySql_25_5_orcl_125_5_inMem_15_DB2_30_5()
   {
      var eventMigrations = EnumerableCE.Create<IEventMigration>(
         Before<E2>.Insert<E3>(),
         Before<E4>.Insert<E5>(),
         Before<E6>.Insert<E7>(),
         Before<E8>.Insert<E9>()
      ).ToArray();

      await AssertUncachedAndCachedAggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 35, mySql: 75, pgSql: 35, sqlite: 50, sqliteMemory: 50).Milliseconds().EnvMultiply(instrumented: 2.5),
         maxCachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 5, mySql: 5, pgSql: 5, sqlite: 10, sqliteMemory: 10).Milliseconds().EnvMultiply(instrumented: 2.5),
         eventMigrations);
   }

   [PCT]  public async Task With_four_migrations_that_change_nothing_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_X_milliseconds_mSSql_30_5_pgSql_30_5_mySql_30_5_orcl_120_5_inMem_15_DB2_30_5()
   {
      var eventMigrations = EnumerableCE.Create<IEventMigration>(
         Before<E3>.Insert<E1>(),
         Before<E5>.Insert<E1>(),
         Before<E7>.Insert<E1>(),
         Before<E9>.Insert<E1>()
      ).ToArray();

      await AssertUncachedAndCachedAggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 30, mySql: 30, pgSql: 30, sqlite: 35, sqliteMemory: 35).Milliseconds().EnvMultiply(instrumented: 2.5),
         maxCachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 5, mySql: 5, pgSql: 5, sqlite: 10, sqliteMemory: 10).Milliseconds().EnvMultiply(instrumented: 2),
         eventMigrations);
   }

   [PCT]  public async Task When_there_are_no_migrations_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_Y_milliseconds()
   {
      var eventMigrations = EnumerableCE.Create<IEventMigration>().ToArray();
      await AssertUncachedAndCachedAggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 25, mySql: 45, pgSql: 20, sqlite: 25, sqliteMemory: 25).Milliseconds().EnvMultiply(instrumented: 3),
         maxCachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 8, mySql: 8, pgSql: 8, sqlite: 15, sqliteMemory: 15).Milliseconds().EnvMultiply(instrumented: 2.5),
         eventMigrations);
   }
}