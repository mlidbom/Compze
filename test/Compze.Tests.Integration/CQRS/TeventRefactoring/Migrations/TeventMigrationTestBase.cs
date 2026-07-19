using Compze.Abstractions.Time.Public;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.Tests.Infrastructure;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Must;
using Compze.Tessaging.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore;
using Compze.Teventive.TeventStore.Abstractions.Public;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;
using Compze.Teventive.TeventStore.Wiring;

// ReSharper disable AccessToModifiedClosure

namespace Compze.Tests.Integration.CQRS.TeventRefactoring.Migrations;

//refactor: this test. It is too monolithic and hard to read and extend.
public abstract class TeventMigrationTestBase : UniversalTestBase
{
   static ILogger Log => CompzeLogger.For<TeventMigrationTestBase>();

   internal static async Task RunMigrationTestAsync(params MigrationScenario[] scenarios)
   {
      Log.Info($"###############$$$$$$$Running {scenarios.Length} scenario(s)");

      IList<ITeventMigration> migrations = new List<ITeventMigration>();
      var container = CreateContainerForTeventStoreType(() => migrations.ToArray());
      await using var locator = container;
      await UtcTimeSource.Test.FrozenAtUtcNow().RunAsync(async () =>
      {
         var scenarioIndex = 1;

         foreach(var migrationScenario in scenarios)
         {
            await UtcTimeSource.Test.FrozenAtUtc(UtcTimeSource.UtcNow + 1.Hours()).RunAsync(async () =>
            {
               migrations = migrationScenario.Migrations.ToList();
               await RunScenarioWithTeventStoreType(migrationScenario, container, migrations, scenarioIndex++);
               return unit;
            });
         }
      });
   }

   static async Task RunScenarioWithTeventStoreType(MigrationScenario scenario, IDependencyInjectionContainer container, IList<ITeventMigration> migrations, int indexOfScenarioInBatch)
   {
      var startingMigrations = migrations.ToList();
      migrations.Clear();

      IReadOnlyList<ITaggregateTevent> teventsInStoreAtStart;
      {
         using var scope = container.BeginScope(); //Why is this needed? It fails without it but I do not understand why...
         var teventStore = scope.Resolve<ITeventStore>();
         teventsInStoreAtStart = teventStore.ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize().Tevents().ToList();
      }

      Log.Info($"\n########Running Scenario {indexOfScenarioInBatch}");

      var original = UtcTimeSource.Test.FrozenAtUtcNow()
                                  .Run(() =>
                                          TestTaggregate.FromTevents(scenario.TaggregateId, scenario.OriginalHistory)
                                                        .History.ToList());

      Log.Info("Original History: ");
      original.ForEach(e => Log.Info($"      {e}"));

      var initialTaggregate = TestTaggregate.FromTevents(scenario.TaggregateId, scenario.OriginalHistory);
      var expected = TestTaggregate.FromTevents(scenario.TaggregateId, scenario.ExpectedHistory)
                                   .History.ToList();
      var expectedCompleteTeventStoreStream = teventsInStoreAtStart.Concat(expected)
                                                                   .ToList();

      Log.Info("Expected History: ");
      expected.ForEach(e => Log.Info($"      {e}"));

      await UtcTimeSource.Test.FrozenAtUtc(UtcTimeSource.UtcNow + 1.Hours()).RunAsync(async () =>
      {
         container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
                                                                              .Save(initialTaggregate));
         startingMigrations.ForEach(migrations.Add);
         ClearCache(container);

         var migratedHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
                                                                                                    .Get<TestTaggregate>(initialTaggregate.Id))
                                             .History;

         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded un-cached taggregate");

         var migratedCachedHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
                                                                                                          .Get<TestTaggregate>(initialTaggregate.Id))
                                                   .History;
         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedCachedHistory, "Loaded cached taggregate");

         Log.Info("  Streaming all tevents in store");
         var streamedTevents = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStore()
                                                                                                    .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                    .Tevents()
                                                                                                    .ToList());

         AssertStreamsAreIdenticalExceptForEventIds(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store");

         //Make sure that other processes that might be using the same taggregate also keep working as we persist the migrations.
         var clonedContainer = container.CloneAndBuild();
         await using(clonedContainer)
         {
            migratedHistory = clonedContainer.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
                                                                                                               .Get<TestTaggregate>(initialTaggregate.Id))
                                                  .History;
            AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded taggregate");

            Log.Info("  Persisting migrations");
            using var persistScope = container.BeginScope();
            persistScope.Resolver.TeventStore()
                        .PersistMigrations();

            migratedHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
                                                                                                   .Get<TestTaggregate>(initialTaggregate.Id))
                                            .History;
            AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded taggregate");

            migratedHistory = clonedContainer.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
                                                                                                               .Get<TestTaggregate>(initialTaggregate.Id))
                                                  .History;
         }

         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded taggregate");

         Log.Info("Streaming all tevents in store");
         streamedTevents = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStore()
                                                                                                .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                .Tevents()
                                                                                                .ToList());
         AssertStreamsAreIdenticalExceptForEventIds(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store");

         Log.Info("  Disable all migrations so that none are used when reading from the tevent stores");
         migrations.Clear();

         migratedHistory = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
                                                                                                .Get<TestTaggregate>(initialTaggregate.Id))
                                         .History;
         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "loaded taggregate");

         Log.Info("Streaming all tevents in store");
         streamedTevents = container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStore()
                                                                                                .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                .Tevents()
                                                                                                .ToList());
         AssertStreamsAreIdenticalExceptForEventIds(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store");

         Log.Info("Cloning service locator / starting new instance of application");
         var clonedContainer2 = container.CloneAndBuild();
         await using var container2 = clonedContainer2;
         migratedHistory = clonedContainer2.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStoreUpdater()
                                                                                                              .Get<TestTaggregate>(initialTaggregate.Id))
                                                .History;
         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded taggregate");

         Log.Info("Streaming all tevents in store");
         streamedTevents = clonedContainer2.ExecuteUnitOfWork(unitOfWork => unitOfWork.TeventStore()
                                                                                                              .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                              .Tevents()
                                                                                                              .ToList());
         AssertStreamsAreIdenticalExceptForEventIds(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store");
      });
   }

   protected static void ClearCache(IDependencyInjectionContainer container)
   {
      container.ExecuteInIsolatedScope(scope =>
      {
         scope.Resolve<ITeventCache>().Clear();
      });
   }

   protected static IDependencyInjectionContainer CreateContainerForTeventStoreType(Func<IReadOnlyList<ITeventMigration>> migrationsFactory)
   {
      var container = TestEnv.DIContainer.CreateContainerForTesting(registrar => registrar.RequireIntegrationTestTypeMappings(), register => register.TeventStoreForFlexibleTesting(CombinedTestingContainers.TeventStoreConnectionStringName, migrationsFactory));

      return container;
   }

   internal static void AssertStreamsAreIdenticalExceptForEventIds(IReadOnlyList<ITaggregateTevent> expected, IReadOnlyList<ITaggregateTevent> migratedHistory, string descriptionOfHistory)
   {
      Log.Info($"Comparing histories for : {descriptionOfHistory}");
      migratedHistory.ToList().Must()
                     .DeepEqualPrivate(expected.ToList(), config => config.ExcludeMember(it => it.First().Id));
   }
}


