using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Abstractions.Tessaging.Teventive.TEventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TEventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Time.Testing.Public;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Teventive.TeventStore;
using Compze.Tessaging.Teventive.TeventStore.Wiring;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Serialization;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using FluentAssertions;
using FluentAssertions.Extensions;
using Newtonsoft.Json;

// ReSharper disable AccessToModifiedClosure

namespace Compze.Tests.Integration.CQRS.TeventRefactoring.Migrations;

//refactor: this test. It is too monolithic and hard to read and extend.
public abstract class TeventMigrationTestBase : UniversalTestBase
{
   internal async Task RunMigrationTest(params MigrationScenario[] scenarios) => await RunMigrationTest(expectedException: null, scenarios);

   internal async Task RunMigrationTest<TException>(params MigrationScenario[] scenarios) where TException : Exception
      => await RunMigrationTest(expectedException: typeof(TException), scenarios);

   static async Task RunMigrationTest(Type? expectedException, params MigrationScenario[] scenarios)
   {
      using var writer = new DeferredConsoleWriter();

      writer.WriteLine($"###############$$$$$$$Running {scenarios.Length} scenario(s)");

      IList<ITeventMigration> migrations = new List<ITeventMigration>();
      var serviceLocator = CreateServiceLocatorForTeventStoreType(() => migrations.ToArray());
      await using var locator = serviceLocator;
      var timeSource = serviceLocator.Resolve<TestingTimeSource>();
      timeSource.FreezeAtUtcTime("2001-02-02 01:01:01.011111");
      var scenarioIndex = 1;

      try
      {
         foreach(var migrationScenario in scenarios)
         {
            timeSource.FreezeAtUtcTime(timeSource.UtcNow + 1.Hours()); //No time collision between scenarios please.
            migrations = migrationScenario.Migrations.ToList();
            await RunScenarioWithTeventStoreType(migrationScenario, serviceLocator, migrations, scenarioIndex++, writer);
         }

         // If we got here without exception, mark as success
         writer.TestSucceeded();
      }
      catch(Exception ex) when (expectedException != null && expectedException.IsInstanceOfType(ex))
      {
         writer.TestSucceeded();
         throw; // Re-throw so the test framework can verify it
      }
      // Any other exception will bubble up without calling TestSucceeded(), so output will be shown
   }

   static async Task RunScenarioWithTeventStoreType(MigrationScenario scenario, IServiceLocator serviceLocator, IList<ITeventMigration> migrations, int indexOfScenarioInBatch, DeferredConsoleWriter writer)
   {
      var startingMigrations = migrations.ToList();
      migrations.Clear();

      var timeSource = serviceLocator.Resolve<TestingTimeSource>();

      IReadOnlyList<ITaggregateTevent> teventsInStoreAtStart;
      using(serviceLocator.BeginScope()) //Why is this needed? It fails without it but I do not understand why...
      {
         var teventStore = serviceLocator.Resolve<ITeventStore>();
         teventsInStoreAtStart = teventStore.ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize();
      }

      writer.WriteLine($"\n########Running Scenario {indexOfScenarioInBatch}");

      var original = TestTaggregate.FromTevents(TestingTimeSource.FrozenUtcNow(), scenario.TaggregateId, scenario.OriginalHistory)
                                  .History.ToList();
      writer.WriteLine("Original History: ");
      original.ForEach(e => writer.WriteLine($"      {e}"));
      writer.WriteLine();

      var initialTaggregate = TestTaggregate.FromTevents(timeSource, scenario.TaggregateId, scenario.OriginalHistory);
      var expected = TestTaggregate.FromTevents(timeSource, scenario.TaggregateId, scenario.ExpectedHistory)
                                  .History.ToList();
      var expectedCompleteTeventStoreStream = teventsInStoreAtStart.Concat(expected)
                                                                 .ToList();

      writer.WriteLine("Expected History: ");
      expected.ForEach(e => writer.WriteLine($"      {e}"));
      writer.WriteLine();

      timeSource.FreezeAtUtcTime(timeSource.UtcNow + 1.Hours()); //Bump clock to ensure that times will be be wrong unless the time from the original tevents are used..

      serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                           .Save(initialTaggregate));
      startingMigrations.ForEach(migrations.Add);
      ClearCache(serviceLocator);

      var migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                 .Get<TestTaggregate>(initialTaggregate.Id))
                                          .History;

      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded un-cached taggregate", writer);

      var migratedCachedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                       .Get<TestTaggregate>(initialTaggregate.Id))
                                                .History;
      AssertStreamsAreIdentical(expected, migratedCachedHistory, "Loaded cached taggregate", writer);

      writer.WriteLine("  Streaming all tevents in store");
      var streamedTevents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStore>()
                                                                                                .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                .ToList());

      AssertStreamsAreIdentical(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store", writer);

      //Make sure that other processes that might be using the same taggregate also keep working as we persist the migrations.
      var clonedServiceLocator = serviceLocator.Clone();
      await using(clonedServiceLocator)
      {
         migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                            .Get<TestTaggregate>(initialTaggregate.Id))
                                               .History;
         AssertStreamsAreIdentical(expected, migratedHistory, "Loaded taggregate", writer);

         writer.WriteLine("  Persisting migrations");
         using(serviceLocator.BeginScope())
         {
            serviceLocator.Resolve<ITeventStore>()
                          .PersistMigrations();
         }

         migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                .Get<TestTaggregate>(initialTaggregate.Id))
                                         .History;
         AssertStreamsAreIdentical(expected, migratedHistory, "Loaded taggregate", writer);

         migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<ITeventStoreUpdater>()
                                                                                                            .Get<TestTaggregate>(initialTaggregate.Id))
                                               .History;
      }

      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded taggregate", writer);

      writer.WriteLine("Streaming all tevents in store");
      streamedTevents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStore>()
                                                                                            .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                            .ToList());
      AssertStreamsAreIdentical(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store", writer);

      writer.WriteLine("  Disable all migrations so that none are used when reading from the tevent stores");
      migrations.Clear();

      migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStoreUpdater>()
                                                                                             .Get<TestTaggregate>(initialTaggregate.Id))
                                      .History;
      AssertStreamsAreIdentical(expected, migratedHistory, "loaded taggregate", writer);

      writer.WriteLine("Streaming all tevents in store");
      streamedTevents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<ITeventStore>()
                                                                                            .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                            .ToList());
      AssertStreamsAreIdentical(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store", writer);

      writer.WriteLine("Cloning service locator / starting new instance of application");
      var clonedServiceLocator2 = serviceLocator.Clone();
      await using var serviceLocator2 = clonedServiceLocator2;
      migratedHistory = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<ITeventStoreUpdater>()
                                                                                                           .Get<TestTaggregate>(initialTaggregate.Id))
                                             .History;
      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded taggregate", writer);

      writer.WriteLine("Streaming all tevents in store");
      streamedTevents = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<ITeventStore>()
                                                                                                          .ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize()
                                                                                                          .ToList());
      AssertStreamsAreIdentical(expectedCompleteTeventStoreStream, streamedTevents, "Streaming all tevents in store", writer);
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

   internal static void AssertStreamsAreIdentical(IReadOnlyList<ITaggregateTevent> expected, IReadOnlyList<ITaggregateTevent> migratedHistory, string descriptionOfHistory, DeferredConsoleWriter writer)
   {
      try
      {
         expected.ForEach((@tevent, index) =>
         {
            if(@tevent.GetType() != migratedHistory.ElementAt(index)
                                                  .GetType())
            {
               throw new Exception(
                  $"Expected tevent at position {index} to be of type {@tevent.GetType()} but it was of type: {migratedHistory.ElementAt(index).GetType()}");
            }
         });

         migratedHistory.Cast<TaggregateTevent>()
                        .Should().BeEquivalentTo(
                            expected.Cast<TaggregateTevent>(),
                            config => config.PreferringRuntimeMemberTypes()
                                            .WithStrictOrdering()
                                            .ComparingByMembers<TaggregateTevent>()
                                            .Excluding(@tevent => @tevent.TessageId));
      }
      catch(Exception)
      {
         writer.WriteLine($"   Failed comparing with {descriptionOfHistory}");
         writer.WriteLine("   Expected: ");
         expected.ForEach(e => writer.WriteLine($"      {e.ToNewtonSoftDebugString(Formatting.None)}"));
         writer.WriteLine("\n   Actual: ");
         migratedHistory.ForEach(e => writer.WriteLine($"      {e.ToNewtonSoftDebugString(Formatting.None)}"));
         writer.WriteLine("\n");

         throw;
      }
   }
}
