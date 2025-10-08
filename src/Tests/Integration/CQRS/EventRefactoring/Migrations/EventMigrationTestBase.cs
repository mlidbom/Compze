using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Abstractions.Internal.Time;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Teventive.EventStore;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;
using Compze.Testing;
using Compze.Testing.Serialization;
using Compze.Tests.CQRS.EventRefactoring.Migrations;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using FluentAssertions;
using FluentAssertions.Extensions;
using Newtonsoft.Json;
using NUnit.Framework;

// ReSharper disable AccessToModifiedClosure

namespace Compze.Tests.Integration.CQRS.EventRefactoring.Migrations;

//refactor: this test. It is too monolithic and hard to read and extend.
public abstract class EventMigrationTestBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   internal async Task RunMigrationTest(params MigrationScenario[] scenarios)
   {
      Console.WriteLine($"###############$$$$$$$Running {scenarios.Length} scenario(s)");

      IList<IEventMigration> migrations = new List<IEventMigration>();
      var serviceLocator = CreateServiceLocatorForEventStoreType(() => migrations.ToArray());
      await using var locator = serviceLocator;
      var timeSource = serviceLocator.Resolve<TestingTimeSource>();
      timeSource.FreezeAtUtcTime("2001-02-02 01:01:01.011111");
      var scenarioIndex = 1;
      foreach(var migrationScenario in scenarios)
      {
         timeSource.FreezeAtUtcTime(timeSource.UtcNow + 1.Hours()); //No time collision between scenarios please.
         migrations = migrationScenario.Migrations.ToList();
         await RunScenarioWithEventStoreType(migrationScenario, serviceLocator, migrations, scenarioIndex++);
      }
   }

   static async Task RunScenarioWithEventStoreType(MigrationScenario scenario, IServiceLocator serviceLocator, IList<IEventMigration> migrations, int indexOfScenarioInBatch)
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

      Console.WriteLine($"\n########Running Scenario {indexOfScenarioInBatch}");

      var original = TestAggregate.FromEvents(TestingTimeSource.FrozenUtcNow(), scenario.AggregateId, scenario.OriginalHistory)
                                  .History.ToList();
      Console.WriteLine("Original History: ");
      original.ForEach(e => Console.WriteLine($"      {e}"));
      Console.WriteLine();

      var initialAggregate = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.OriginalHistory);
      var expected = TestAggregate.FromEvents(timeSource, scenario.AggregateId, scenario.ExpectedHistory)
                                  .History.ToList();
      var expectedCompleteEventStoreStream = eventsInStoreAtStart.Concat(expected)
                                                                 .ToList();

      Console.WriteLine("Expected History: ");
      expected.ForEach(e => Console.WriteLine($"      {e}"));
      Console.WriteLine();

      timeSource.FreezeAtUtcTime(timeSource.UtcNow + 1.Hours()); //Bump clock to ensure that times will be be wrong unless the time from the original events are used..

      serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                           .Save(initialAggregate));
      startingMigrations.ForEach(migrations.Add);
      ClearCache(serviceLocator);

      var migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                                 .Get<TestAggregate>(initialAggregate.Id))
                                          .History;

      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded un-cached aggregate");

      var migratedCachedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                                       .Get<TestAggregate>(initialAggregate.Id))
                                                .History;
      AssertStreamsAreIdentical(expected, migratedCachedHistory, "Loaded cached aggregate");

      Console.WriteLine("  Streaming all events in store");
      var streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                                .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                .ToList());

      AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store");


      //Make sure that other processes that might be using the same aggregate also keep working as we persist the migrations.
      var clonedServiceLocator = serviceLocator.Clone();
      await using(clonedServiceLocator)
      {
         migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<IEventStoreUpdater>()
                                                                                                            .Get<TestAggregate>(initialAggregate.Id))
                                               .History;
         AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

         Console.WriteLine("  Persisting migrations");
         using(serviceLocator.BeginScope())
         {
            serviceLocator.Resolve<IEventStore>()
                          .PersistMigrations();
         }

         migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                                .Get<TestAggregate>(initialAggregate.Id))
                                         .History;
         AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

         migratedHistory = clonedServiceLocator.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator.Resolve<IEventStoreUpdater>()
                                                                                                            .Get<TestAggregate>(initialAggregate.Id))
                                               .History;
      }
      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

      Console.WriteLine("Streaming all events in store");
      streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                            .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                            .ToList());
      AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store");

      Console.WriteLine("  Disable all migrations so that none are used when reading from the event stores");
      migrations.Clear();

      migratedHistory = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStoreUpdater>()
                                                                                             .Get<TestAggregate>(initialAggregate.Id))
                                      .History;
      AssertStreamsAreIdentical(expected, migratedHistory, "loaded aggregate");

      Console.WriteLine("Streaming all events in store");
      streamedEvents = serviceLocator.ExecuteTransactionInIsolatedScope(() => serviceLocator.Resolve<IEventStore>()
                                                                                            .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                            .ToList());
      AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store");


      Console.WriteLine("Cloning service locator / starting new instance of application");
      var clonedServiceLocator2 = serviceLocator.Clone();
      await using var serviceLocator2 = clonedServiceLocator2;
      migratedHistory = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<IEventStoreUpdater>()
                                                                                                           .Get<TestAggregate>(initialAggregate.Id))
                                             .History;
      AssertStreamsAreIdentical(expected, migratedHistory, "Loaded aggregate");

      Console.WriteLine("Streaming all events in store");
      streamedEvents = clonedServiceLocator2.ExecuteTransactionInIsolatedScope(() => clonedServiceLocator2.Resolve<IEventStore>()
                                                                                                          .ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize()
                                                                                                          .ToList());
      AssertStreamsAreIdentical(expectedCompleteEventStoreStream, streamedEvents, "Streaming all events in store");
   }
   protected static void ClearCache(IServiceLocator serviceLocator)
   {
      serviceLocator.ExecuteInIsolatedScope(() =>
      {
            serviceLocator.Resolve<IEventCache>().Clear();
      });
   }

   protected static IServiceLocator CreateServiceLocatorForEventStoreType(Func<IReadOnlyList<IEventMigration>> migrationsfactory)
   {
      var serviceLocator = TestingContainerFactory.CreateServiceLocatorForTesting(
         endpointBuilder =>
            endpointBuilder.Container.RegisterEventStoreForFlexibleTesting(TestWiringHelper.EventStoreConnectionStringName, migrationsfactory));

      return serviceLocator;
   }

   protected static void AssertStreamsAreIdentical(IReadOnlyList<IAggregateEvent> expected, IReadOnlyList<IAggregateEvent> migratedHistory, string descriptionOfHistory)
   {
      try
      {
         expected.ForEach(
            (@event, index) =>
            {
               if(@event.GetType() != migratedHistory.ElementAt(index)
                                                     .GetType())
               {
                  throw new AssertionException(
                     $"Expected event at position {index} to be of type {@event.GetType()} but it was of type: {migratedHistory.ElementAt(index) .GetType()}");
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
         Console.WriteLine($"   Failed comparing with {descriptionOfHistory}");
         Console.WriteLine("   Expected: ");
         expected.ForEach(e => Console.WriteLine($"      {e.ToNewtonSoftDebugString(Formatting.None)}"));
         Console.WriteLine("\n   Actual: ");
         migratedHistory.ForEach(e => Console.WriteLine($"      {e.ToNewtonSoftDebugString(Formatting.None)}"));
         Console.WriteLine("\n");

         throw;
      }
   }
}