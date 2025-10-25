using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using FluentAssertions;
using System;
using System.Linq;
using System.Transactions;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Integration.CQRS;

interface ISomeTevent : IAggregateTevent;

class SomeTevent : AggregateTevent, ISomeTevent
{
   public SomeTevent(Guid aggregateId, int version) : base(aggregateId)
   {
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateTevent)this).SetAggregateVersionInternal(version);
      ((IMutableAggregateTevent)this).SetUtcTimeStampInternal(new DateTime(UtcTimeStamp.Year, UtcTimeStamp.Month, UtcTimeStamp.Day, UtcTimeStamp.Hour, UtcTimeStamp.Minute, UtcTimeStamp.Second, DateTimeKind.Utc));
#pragma warning restore CS0618 // Type or member is obsolete
   }
}

public class EventStoreTests : UniversalTestBase
{
   readonly IServiceLocator _serviceLocator = TestEnv.DIContainer.SetupTestingServiceLocator();
   IEventStore EventStore => _serviceLocator.EventStore();

   protected override void DisposeInternal() => _serviceLocator.Dispose();

   [PCT]
   public void StreamEventsSinceReturnsWholeEventLogWhenFromEventIdIsNull() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var aggregateId = Guid.NewGuid();
      TransactionScopeCe.Execute(() => EventStore.SaveSingleAggregateEvents(1.Through(10)
                                                                             .Select(i => new SomeTevent(aggregateId, i)).ToList()));
      var stream = EventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();

      stream.Should()
            .HaveCount(10);
   });

   [PCT]
   public void StreamEventsSinceReturnsWholeEventLogWhenFetchingALargeNumberOfEvents_EnsureBatchingDoesNotBreakThings() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      const int batchSize = 100;
      const int moreEventsThanTheBatchSizeForStreamingEvents = batchSize + 10;
      var aggregateId = Guid.NewGuid();

      TransactionScopeCe.Execute(() => EventStore.SaveSingleAggregateEvents(1.Through(moreEventsThanTheBatchSizeForStreamingEvents)
                                                                             .Select(i => new SomeTevent(aggregateId, i)).ToList()));

      var stream = EventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize(batchSize: batchSize)
                             .ToList();

      var currentEventNumber = 0;
      stream.Should()
            .HaveCount(moreEventsThanTheBatchSizeForStreamingEvents);
      foreach(var aggregateEvent in stream)
      {
         aggregateEvent.AggregateVersion.Should()
                       .Be(++currentEventNumber, "Incorrect event version detected");
      }
   });

   [PCT]
   public void DeleteEventsDeletesTheEventsForOnlyTheSpecifiedAggregate() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithEvents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeTevent(aggregateId, j))
                                                           .ToList();
                                                });

      TransactionScopeCe.Execute(() => aggregatesWithEvents.ForEach(it => EventStore.SaveSingleAggregateEvents(it.Value)));
      var toRemove = aggregatesWithEvents[2][0]
        .AggregateId;
      aggregatesWithEvents.Remove(2);

      TransactionScopeCe.Execute(() => EventStore.DeleteAggregate(toRemove));

      aggregatesWithEvents.Select(kvp => EventStore.GetAggregateHistory(kvp.Value[0].AggregateId))
                          .ForEach(stream => stream.Should().HaveCount(10));

      EventStore.GetAggregateHistory(toRemove)
                .Should()
                .BeEmpty();
   });

   [PCT]
   public void GetListOfAggregateIds() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithEvents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeTevent(aggregateId, j))
                                                           .ToList();
                                                });

      TransactionScopeCe.Execute(() => aggregatesWithEvents.ForEach(it => EventStore.SaveSingleAggregateEvents(it.Value)));

      var allAggregateIds = EventStore.StreamAggregateIdsInCreationOrder()
                                      .ToList();
      allAggregateIds.Should().HaveCount(aggregatesWithEvents.Count);
   });

   //Todo: This does not check that only aggregates of the correct type are returned since there are only events of type SomeEvent in the store..
   [PCT]
   public void GetListOfAggregateIdsUsingEventType() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithEvents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeTevent(aggregateId, j))
                                                           .ToList();
                                                });

      TransactionScopeCe.Execute(() => aggregatesWithEvents.ForEach(it => EventStore.SaveSingleAggregateEvents(it.Value)));
      var allAggregateIds = EventStore.StreamAggregateIdsInCreationOrder<ISomeTevent>()
                                      .ToList();
      allAggregateIds.Should().HaveCount(aggregatesWithEvents.Count);
   });

   [PCT]
   public void Does_not_call_db_in_constructor() => _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.Resolve<IEventStoreUpdater>());

   [PCT]
   public void ShouldNotCacheEventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var eventStore = _serviceLocator.EventStore();

      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

      using(new TransactionScope())
      {
         ((IEventStored)user).Commit(eventStore.SaveSingleAggregateEvents);
         eventStore.GetAggregateHistory(user.Id);
         eventStore.GetAggregateHistory(user.Id).Should().NotBeEmpty();
      }

      eventStore.GetAggregateHistory(user.Id).Should().BeEmpty();
   });

   [PCT]
   public void ShouldCacheEventsBetweenInstancesTransaction()
   {
      using var serviceLocator = TestEnv.DIContainer.SetupTestingServiceLocator();

      var user = new User();
      using(serviceLocator.BeginScope())
      {
         var eventStore = serviceLocator.EventStore();

         user.Register("email@email.se", "password", Guid.NewGuid());

         TransactionScopeCe.Execute(() =>
         {
            ((IEventStored)user).Commit(eventStore.SaveSingleAggregateEvents);
            eventStore.GetAggregateHistory(user.Id);
            eventStore.GetAggregateHistory(user.Id).Should().NotBeEmpty();
         });
      }

      var firstRead = serviceLocator.ExecuteInIsolatedScope(() => serviceLocator.EventStore().GetAggregateHistory(user.Id).Single());

      var secondRead = serviceLocator.ExecuteInIsolatedScope(() => serviceLocator.EventStore().GetAggregateHistory(user.Id).Single());

      firstRead.Should().BeSameAs(secondRead);
   }
}
