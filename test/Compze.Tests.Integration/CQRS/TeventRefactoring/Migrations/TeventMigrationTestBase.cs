using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Core.Time.Public;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Teventive.TeventStore;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Underscore;
using Compze.Utilities.Logging;
using Compze.Utilities.SystemCE;
using Compze.Utilities.Testing.Must;

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
      var serviceLocator = CreateServiceLocatorForTeventStoreType(() => migrations.ToArray());
      await using var locator = serviceLocator;
      await UtcTimeSource.Test.FrozenAtUtcNow().RunAsync(async () =>
      {
         var scenarioIndex = 1;

         foreach(var migrationScenario in scenarios)
         {
            await UtcTimeSource.Test.FrozenAtUtc(UtcTimeSource.UtcNow + 1.Hours()).RunAsync(async () =>
            {
               migrations = migrationScenario.Migrations.ToList();
               await RunScenarioWithTeventStoreType(migrationScenario, serviceLocator, migrations, scenarioIndex++);
               return unit.Value;
            });
         }
      });
   }

   static async Task RunScenarioWithTeventStoreType(MigrationScenario scenario, IServiceLocator serviceLocator, IList<ITeventMigration> migrations, int indexOfScenarioInBatch)
   {
      var startingMigrations = migrations.ToList();
      migrations.Clear();

      IReadOnlyList<ITaggregateTevent> teventsInStoreAtStart;
      using(serviceLocator.BeginScope()) //Why is this needed? It fails without it but I do not understand why...
      {
         var teventStore = serviceLocator.Resolve<ITeventStore>();
         teventsInStoreAtStart = teventStore.ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize();
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
         serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                              .Save(initialTaggregate));
         startingMigrations.ForEach(migrations.Add);
         ClearCache(serviceLocator);

         var migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                    .Get<TestTaggregate>(initialTaggregate.Id))
                                             .History;

         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded un-cached taggregate");

         var migratedCachedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                          .Get<TestTaggregate>(initialTaggregate.Id))
                                                   .History;
         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedCachedHistory, "Loaded cached taggregate");

         Log.Info("  Streaming all tevents in store");
         var streamedTevents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStore>()
                                                                                                    .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                    .ToList());

         AssertStreamsAreIdenticalExceptForEventIds(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store");

         //Make sure that other processes that might be using the same taggregate also keep working as we persist the migrations.
         var clonedServiceLocator = serviceLocator.Clone();
         await using(clonedServiceLocator)
         {
            migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                               .Get<TestTaggregate>(initialTaggregate.Id))
                                                  .History;
            AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded taggregate");

            Log.Info("  Persisting migrations");
            using(serviceLocator.BeginScope())
            {
               serviceLocator.Resolve<ITeventStore>()
                             .PersistMigrations();
            }

            migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                   .Get<TestTaggregate>(initialTaggregate.Id))
                                            .History;
            AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded taggregate");

            migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                               .Get<TestTaggregate>(initialTaggregate.Id))
                                                  .History;
         }

         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded taggregate");

         Log.Info("Streaming all tevents in store");
         streamedTevents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStore>()
                                                                                                .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                .ToList());
         AssertStreamsAreIdenticalExceptForEventIds(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store");

         Log.Info("  Disable all migrations so that none are used when reading from the tevent stores");
         migrations.Clear();

         migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                .Get<TestTaggregate>(initialTaggregate.Id))
                                         .History;
         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "loaded taggregate");

         Log.Info("Streaming all tevents in store");
         streamedTevents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStore>()
                                                                                                .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                .ToList());
         AssertStreamsAreIdenticalExceptForEventIds(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store");

         Log.Info("Cloning service locator / starting new instance of application");
         var clonedServiceLocator2 = serviceLocator.Clone();
         await using var serviceLocator2 = clonedServiceLocator2;
         migratedHistory = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<ITeventStoreUpdater>()
                                                                                                              .Get<TestTaggregate>(initialTaggregate.Id))
                                                .History;
         AssertStreamsAreIdenticalExceptForEventIds(expected, migratedHistory, "Loaded taggregate");

         Log.Info("Streaming all tevents in store");
         streamedTevents = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<ITeventStore>()
                                                                                                              .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                              .ToList());
         AssertStreamsAreIdenticalExceptForEventIds(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store");
      });
   }

   protected static void ClearCache(IServiceLocator serviceLocator)
   {
      serviceLocator.ExecuteInIsolatedScope(() =>
      {
         serviceLocator.Resolve<ITeventCache>().Clear();
      });
   }

   protected static IServiceLocator CreateServiceLocatorForTeventStoreType(Func<IReadOnlyList<ITeventMigration>> migrationsFactory)
   {
      var serviceLocator = TestEnv.DIContainer.CreateServiceLocatorForTesting(register => register.TeventStoreForFlexibleTesting(DiContainerExtensions.TeventStoreConnectionStringName, migrationsFactory));

      return serviceLocator;
   }

   internal static void AssertStreamsAreIdenticalExceptForEventIds(IReadOnlyList<ITaggregateTevent> expected, IReadOnlyList<ITaggregateTevent> migratedHistory, string descriptionOfHistory)
   {
      Log.Info($"Comparing histories for : {descriptionOfHistory}");
      migratedHistory.ToList().Must()
                     .DeepEqualPrivate(expected.ToList(), config => config.ExcludeTypeMember(it => it.First().Id));
   }
}
