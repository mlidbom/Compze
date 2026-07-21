using Compze.Abstractions;
using Compze.Abstractions.Time;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Internals.Testing.Performance;
using Compze.Tests.Common;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.CQRS.TeventRefactoring.Migrations;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Contracts;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations.Tevents;
using Compze.Teventive;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations;

namespace Compze.Tests.Performance.Internals.CQRS.TeventRefactoring.Migrations;

[LongRunning]
public class TeventMigrationPerformanceTest : TeventMigrationTestBase
{
   readonly TestTaggregate _taggregate;
   readonly IDependencyInjectionContainer? _container;
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

      _currentMigrations = Enumerable.Empty<ITeventMigration>().ToList();
      _container = CreateContainerForTeventStoreType(migrationsFactory: () => _currentMigrations);

      _container.ExecuteUnitOfWork(unitOfWork => ((ITaggregate)_taggregate).Commit(unitOfWork.TeventStore().SaveSingleTaggregateTevents));
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null)
         await _container.DisposeAsync();
   }

   async Task AssertUncachedAndCachedTaggregateLoadTimes(TimeSpan maxUncachedLoadTime, TimeSpan maxCachedLoadTime, IReadOnlyList<ITeventMigration> migrations)
   {
      _currentMigrations = migrations;

      IDependencyInjectionContainer? clonedContainer = null;

      await TimeAsserter.ExecuteAsync(
         description: "Uncached loading",
         maxTotal: maxUncachedLoadTime,
         setup: () => clonedContainer = _container!.CloneAndBuild(),
         tearDownAsync: async Task () => await clonedContainer._assert().NotNull().DisposeAsync(),
         action: () =>
         {
            LoadWithClonedContainer(clonedContainer!);
            return Task.CompletedTask;
         });

      await using(clonedContainer = _container!.CloneAndBuild())
      {
         LoadWithClonedContainer(clonedContainer); //Warm up cache

         TimeAsserter.Execute(
            description: "Cached loading",
            maxTotal: maxCachedLoadTime,
            action: () => LoadWithClonedContainer(clonedContainer));
      }

      return;

      void LoadWithClonedContainer(IDependencyInjectionContainer container) => container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
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