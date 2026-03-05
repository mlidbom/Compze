using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Core.Time.Public;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations.Tevents;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.CQRS.TeventRefactoring.Migrations;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Contracts;

namespace Compze.Tests.Performance.Internals.CQRS.TeventRefactoring.Migrations;

[LongRunning]
public class TeventMigrationPerformanceTest : TeventMigrationTestBase
{
   readonly TestTaggregate _taggregate;
   readonly IServiceLocator? _container;
   IReadOnlyList<ITeventMigration> _currentMigrations;
   public TeventMigrationPerformanceTest()
   {
      var historyTypes = EnumerableCE.OfTypes<Ec1>()
                                     .Concat(
                                         1.Through(10)
                                          .SelectMany(
                                              _ => 1.Through(10)
                                                    .Select(_ => typeof(E1))
                                                    .Concat(EnumerableCE.OfTypes<E2, E4, E6, E8>()))).ToList();


      _taggregate = UtcTimeSource.Test.FrozenAtUtcNow()
                                     .Run(() => TestTaggregate.FromTevents(new TaggregateId(), historyTypes));
      var history = _taggregate.History.Cast<TaggregateTevent>().ToList();

      _currentMigrations = Enumerable.Empty<ITeventMigration>().ToList();
      _container = CreateServiceLocatorForTeventStoreType(migrationsFactory: () => _currentMigrations);

      _container.ExecuteTransactionInIsolatedScope(() => _container.Resolve<ITeventStore>().SaveSingleTaggregateTevents(history));
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null)
         await _container.DisposeAsync();
   }

   async Task AssertUncachedAndCachedTaggregateLoadTimes(TimeSpan maxUncachedLoadTime, TimeSpan maxCachedLoadTime, IReadOnlyList<ITeventMigration> migrations)
   {
      _currentMigrations = migrations;

      IServiceLocator? clonedLocator = null;

      await TimeAsserter.ExecuteAsync(
         description: "Uncached loading",
         maxTotal: maxUncachedLoadTime,
         setup: () => clonedLocator = _container!.Clone(),
         tearDownAsync: async Task () => await clonedLocator._assert().NotNull().DisposeAsync(),
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

      void LoadWithCloneLocator(IServiceLocator locator) => locator.ExecuteTransactionInIsolatedScope(() => locator.Resolve<ITeventStoreUpdater>()
                                                                                                                   .Get<TestTaggregate>(_taggregate.Id));
   }

   [PCT]  public async Task With_four_migrations_mutation_that_all_actually_changes_things_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_Y_milliseconds()
   {
      var teventMigrations = EnumerableCE.Create<ITeventMigration>(
         Before<E2>.Insert<E3>(),
         Before<E4>.Insert<E5>(),
         Before<E6>.Insert<E7>(),
         Before<E8>.Insert<E9>()
      ).ToArray();

      await AssertUncachedAndCachedTaggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 35, mySql: 75, pgSql: 35, sqlite: 50, sqliteMemory: 50).Milliseconds().EnvMultiply(instrumented: 2.5),
         maxCachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 2, mySql: 5, pgSql: 5, sqlite: 10, sqliteMemory: 10).Milliseconds().EnvMultiply(instrumented: 2.5),
         teventMigrations);
   }

   [PCT]  public async Task With_four_migrations_that_change_nothing_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_X_milliseconds()
   {
      var teventMigrations = EnumerableCE.Create<ITeventMigration>(
         Before<E3>.Insert<E1>(),
         Before<E5>.Insert<E1>(),
         Before<E7>.Insert<E1>(),
         Before<E9>.Insert<E1>()
      ).ToArray();

      await AssertUncachedAndCachedTaggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 8, mySql: 40, pgSql: 40, sqlite: 40, sqliteMemory: 40).Milliseconds().EnvMultiply(instrumented: 2.5),
         maxCachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 2, mySql: 5, pgSql: 5, sqlite: 10, sqliteMemory: 10).Milliseconds().EnvMultiply(instrumented: 2),
         teventMigrations);
   }

   [PCT]  public async Task When_there_are_no_migrations_uncached_loading_takes_less_than_X_milliseconds_cached_less_than_Y_milliseconds()
   {
      var teventMigrations = EnumerableCE.Create<ITeventMigration>().ToArray();
      await AssertUncachedAndCachedTaggregateLoadTimes(
         maxUncachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 30, mySql: 45, pgSql: 40, sqlite: 25, sqliteMemory: 25).Milliseconds().EnvMultiply(instrumented: 3),
         maxCachedLoadTime: TestEnv.SqlLayer.ValueFor(msSql: 2, mySql: 3, pgSql: 8, sqlite: 15, sqliteMemory: 15).Milliseconds().EnvMultiply(instrumented: 2.5),
         teventMigrations);
   }
}