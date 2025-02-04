using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.GenericAbstractions.Time;
using Compze.Persistence.Common.DependencyInjection;
using Compze.Persistence.EventStore;
using Compze.Persistence.EventStore.Refactoring.Migrations;
using Compze.Refactoring.Naming;
using Compze.SystemCE.LinqCE;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Tests.CQRS.EventRefactoring.Migrations;
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

      serviceLocator.Resolve<ITypeMappingRegistar>()
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.TestAggregate>("dbc5cd48-bc09-4d96-804d-6712493a413d")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E1>("cdb56e08-9ccb-497a-89cd-230913a51877")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E2>("808a5fed-4925-4b2c-8992-fd75521959e6")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E3>("9297ccdd-0a0b-4632-8c86-2634f75822bf")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E4>("aa67591a-2a91-4e74-9cc3-0991f72473bc")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E5>("32979722-64d1-4113-af1d-c5f7c2c6862c")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E6>("08bfb660-adc4-480f-82d5-db64fa9a0ac5")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E7>("d8a2ea4f-7dad-4658-8530-a50f092f0640")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E8>("70424c93-f24c-44c9-a1d6-fb2d6fe83e0a")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.E9>("ec965ddd-5a8a-4fef-890f-4f302069e8ba")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.Ec1>("117fc595-4756-4695-a907-43d0501bf32c")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.Ec2>("3d0e3a47-989d-4096-9389-79d6960ee6d6")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.Ec3>("76b2bbce-b5b4-4293-b707-85cbbaeb7916")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.EcAbstract>("74797038-5f9b-4660-b853-fa81ad67f193")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.Events.Ef>("19f36c9a-6f42-429a-9d43-26532e718ceb")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.IRootEvent>("a846112e-86ce-4dc5-ac7b-97bb44f8e1ce")
                    .Map<Compze.Tests.CQRS.EventRefactoring.Migrations.RootEvent>("a3714dd8-1c20-47be-bb5a-a17ee2c5656f")
                    .Map<IUserEvent>("059a8d68-9b84-4e6b-85b6-fb3e0f7d9d6f")
                    .Map<MigratedAfterUserChangedEmailEvent>("ebda8f29-0e76-493f-b4d5-220b9605de13")
                    .Map<MigratedBeforeUserRegisteredEvent>("3b3477ab-014b-4dbf-921d-8569d7e934e2")
                    .Map<MigratedReplaceUserChangedPasswordEvent>("fa51dab5-d012-491a-b73e-5b343d9aa2d0")
                    .Map<UserChangedEmail>("67c06a44-56eb-4b67-b6e5-ef125653ed7c")
                    .Map<UserChangedPassword>("bbcad7d4-e5f6-45b1-8dd5-99d54b048e3a")
                    .Map<UserEvent>("507c052d-eeaf-402f-9f2b-91941118caf2")
                    .Map<UserRegistered>("02feaed0-b540-4402-92b2-30073db53fa1");

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
                            config => config.RespectingRuntimeTypes()
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