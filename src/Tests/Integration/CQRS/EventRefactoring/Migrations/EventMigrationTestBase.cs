using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;
using Compze.Abstractions.Time.Testing.Public;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Teventive.EventStore;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Tests.Common.CQRS.EventRefactoring.Migrations;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Serialization;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using FluentAssertions;
using FluentAssertions.Extensions;
using Newtonsoft.Json;

// ReSharper disable AccessToModifiedClosure

namespace Compze.Tests.Integration.CQRS.EventRefactoring.Migrations;

//refactor: this test. It is too monolithic and hard to read and extend.
public abstract class EventMigrationTestBase : UniversalTestBase
{
   internal async Task RunMigrationTest(params MigrationScenario[] scenarios) => await RunMigrationTest(expectedException: null, scenarios);

   internal async Task RunMigrationTest<TException>(params MigrationScenario[] scenarios) where TException : Exception
      => await RunMigrationTest(expectedException: typeof(TException), scenarios);

   static async Task RunMigrationTest(Type? expectedException, params MigrationScenario[] scenarios)
   {
      using var writer = new DeferredConsoleWriter();

      writer.WriteLine($"###############$$$$$$$Running {scenarios.Length} scenario(s)");

      IList<IEventMigration> migrations = new List<IEventMigration>();
      var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations.ToArray());
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
            await RunScenarioWithEventStoreType(migrationScenario, serviceLocator, migrations, scenarioIndex++, writer);
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

   static async Task RunScenarioWithEventStoreType(MigrationScenario scenario, IServiceLocator serviceLocator, IList<IEventMigration> migrations, int indexOfScenarioInBatch, DeferredConsoleWriter writer)
   {
      var startingMigrations = migrations.ToList();
      migrations.Clear();

      var timeSource = serviceLocator.Resolve<TestingTimeSource>();

      IReadOnlyList<IAggregateEvent> eventsInStoreAtStart;
      using(serviceLocator.BeginScope()) //Why is this needed? It fails without it but I do not understand why...
      {
         var eventStore = serviceLocator.Resolve<IEventStore>();
         eventsInStoreAtStart = eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();
      }

      writer.WriteLine($"\n########Running Scenario {indexOfScenarioInBatch}");

      var original = TestAggregate.FromEvents(TestingTimeSource.FrozenUtcNow(), scenario.AggregateId, scenario.OriginalHistory)
                                  .History.ToList();
      writer.WriteLine("Original History: ");
      original.ForEach(e => writer.WriteLine($"      {e}"));
      writer.WriteLine();

      var initialAggregate = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.OriginalHistory);
      var expected = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.ExpectedHistory)
                                  .History.ToList();
      var expectedCompleteEventStoreStream = eventsInStoreAtStart.Concat(expected)
                                                                 .ToList();

      writer.WriteLine("Expected History: ");
      expected.ForEach(e => writer.WriteLine($"      {e}"));
      writer.WriteLine();

      timeSource.FreezeAtUtcTime(timeSource.UtcNow + 1.Hours()); //Bump clock to ensure that times will be be wrong unless the time from the original events are used..

      serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                           .Save(initialAggregate));
      startingMigrations.ForEach(migrations.Add);
      ClearCache(serviceLocator);

      var migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                                 .Get<TestAggregate>(initialAggregate.Id))
                                          .History;

      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded un-cached aggregate", writer);

      var migratedCachedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                                       .Get<TestAggregate>(initialAggregate.Id))
                                                .History;
      AssertStreamsAreIdentical(expected, migratedCachedHistory, "Loaded cached aggregate", writer);

      writer.WriteLine("  Streaming all events in store");
      var streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                                .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                .ToList());

      AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store", writer);

      //Make sure that other processes that might be using the same aggregate also keep working as we persist the migrations.
      var clonedServiceLocator = serviceLocator.Clone();
      await using(clonedServiceLocator)
      {
         migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<IEventStoreUpdater>()
                                                                                                            .Get<TestAggregate>(initialAggregate.Id))
                                               .History;
         AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate", writer);

         writer.WriteLine("  Persisting migrations");
         using(serviceLocator.BeginScope())
         {
            serviceLocator.Resolve<IEventStore>()
                          .PersistMigrations();
         }

         migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                                .Get<TestAggregate>(initialAggregate.Id))
                                         .History;
         AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate", writer);

         migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<IEventStoreUpdater>()
                                                                                                            .Get<TestAggregate>(initialAggregate.Id))
                                               .History;
      }

      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate", writer);

      writer.WriteLine("Streaming all events in store");
      streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                            .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                            .ToList());
      AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store", writer);

      writer.WriteLine("  Disable all migrations so that none are used when reading from the event stores");
      migrations.Clear();

      migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                             .Get<TestAggregate>(initialAggregate.Id))
                                      .History;
      AssertStreamsAreIdentical(expected, migratedHistory, "loaded aggregate", writer);

      writer.WriteLine("Streaming all events in store");
      streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                            .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                            .ToList());
      AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store", writer);

      writer.WriteLine("Cloning service locator / starting new instance of application");
      var clonedServiceLocator2 = serviceLocator.Clone();
      await using var serviceLocator2 = clonedServiceLocator2;
      migratedHistory = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<IEventStoreUpdater>()
                                                                                                           .Get<TestAggregate>(initialAggregate.Id))
                                             .History;
      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate", writer);

      writer.WriteLine("Streaming all events in store");
      streamedEvents = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<IEventStore>()
                                                                                                          .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                          .ToList());
      AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store", writer);
   }

   protected static void ClearCache(IServiceLocator serviceLocator)
   {
      serviceLocator.ExecuteInIsolatedScope(() =>
      {
         serviceLocator.Resolve<IEventCache>().Clear();
      });
   }

   protected static IServiceLocator CreateServiceLocatorForEventStoreType(Func<IReadOnlyList<IEventMigration>> migrationsFactory)
   {
      var serviceLocator = TestEnv.DIContainer.CreateServiceLocatorForTesting(register => register.EventStoreForFlexibleTesting(DiContainerExtensions.EventStoreConnectionStringName, migrationsFactory));

      return serviceLocator;
   }

   internal static void AssertStreamsAreIdentical(IReadOnlyList<IAggregateEvent> expected, IReadOnlyList<IAggregateEvent> migratedHistory, string descriptionOfHistory, DeferredConsoleWriter writer)
   {
      try
      {
         expected.ForEach((@event, index) =>
         {
            if(@event.GetType() != migratedHistory.ElementAt(index)
                                                  .GetType())
            {
               throw new Exception(
                  $"Expected event at position {index} to be of type {@event.GetType()} but it was of type: {migratedHistory.ElementAt(index).GetType()}");
            }
         });

         migratedHistory.Cast<AggregateEvent>()
                        .Should().BeEquivalentTo(
                            expected.Cast<AggregateEvent>(),
                            config => config.PreferringRuntimeMemberTypes()
                                            .WithStrictOrdering()
                                            .ComparingByMembers<AggregateEvent>()
                                            .Excluding(@event => @event.MessageId));
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
