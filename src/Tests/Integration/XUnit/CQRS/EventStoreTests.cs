using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using FluentAssertions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Xunit;

namespace Compze.Tests.Integration.XUnit.CQRS;

interface ISomeEvent : IAggregateEvent;

class SomeEvent : AggregateEvent, ISomeEvent
{
   public SomeEvent(Guid aggregateId, int version) : base(aggregateId)
   {
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateEvent)this).SetAggregateVersionInternal(version);
      ((IMutableAggregateEvent)this).SetUtcTimeStampInternal(new DateTime(UtcTimeStamp.Year, UtcTimeStamp.Month, UtcTimeStamp.Day, UtcTimeStamp.Hour, UtcTimeStamp.Minute, UtcTimeStamp.Second, DateTimeKind.Utc));
#pragma warning restore CS0618 // Type or member is obsolete
   }
}

public class EventStoreTests : UniversalTestBase, IAsyncLifetime
{
   IEventStore EventStore => _serviceLocator.EventStore();

   IServiceLocator _serviceLocator = null!;

   public async ValueTask InitializeAsync() => await ValueTask.CompletedTask;

   public async ValueTask DisposeAsync()
   {
      await _serviceLocator.DisposeAsync();
      GC.SuppressFinalize(this);
   }

   IServiceLocator Init(PluggableComponentTestContext context) =>
      _serviceLocator = context.CreateServiceLocator();

   [PluggableComponentsTheory]
   public void StreamEventsSinceReturnsWholeEventLogWhenFromEventIdIsNull(PluggableComponentTestContext context) => Init(context).ExecuteInIsolatedScope(() =>
   {
      var aggregateId = Guid.NewGuid();
      TransactionScopeCe.Execute(() => EventStore.SaveSingleAggregateEvents(1.Through(10)
                                                                             .Select(i => new SomeEvent(aggregateId, i)).ToList()));
      var stream = EventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();

      stream.Should()
            .HaveCount(10);
   });

   [PluggableComponentsTheory]
   public void StreamEventsSinceReturnsWholeEventLogWhenFetchingALargeNumberOfEvents_EnsureBatchingDoesNotBreakThings(PluggableComponentTestContext context) => Init(context).ExecuteInIsolatedScope(() =>
   {
      const int batchSize = 100;
      const int moreEventsThanTheBatchSizeForStreamingEvents = batchSize + 10;
      var aggregateId = Guid.NewGuid();

      TransactionScopeCe.Execute(() => EventStore.SaveSingleAggregateEvents(1.Through(moreEventsThanTheBatchSizeForStreamingEvents)
                                                                             .Select(i => new SomeEvent(aggregateId, i)).ToList()));

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

   [PluggableComponentsTheory]
   public void DeleteEventsDeletesTheEventsForOnlyTheSpecifiedAggregate(PluggableComponentTestContext context) => Init(context).ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithEvents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeEvent(aggregateId, j))
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

   [PluggableComponentsTheory]
   public void GetListOfAggregateIds(PluggableComponentTestContext context) => Init(context).ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithEvents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeEvent(aggregateId, j))
                                                           .ToList();
                                                });

      TransactionScopeCe.Execute(() => aggregatesWithEvents.ForEach(it => EventStore.SaveSingleAggregateEvents(it.Value)));

      var allAggregateIds = EventStore.StreamAggregateIdsInCreationOrder()
                                      .ToList();
      allAggregateIds.Should().HaveCount(aggregatesWithEvents.Count);
   });

   //Todo: This does not check that only aggregates of the correct type are returned since there are only events of type SomeEvent in the store..
   [PluggableComponentsTheory]
   public void GetListOfAggregateIdsUsingEventType(PluggableComponentTestContext context) => Init(context).ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithEvents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeEvent(aggregateId, j))
                                                           .ToList();
                                                });

      TransactionScopeCe.Execute(() => aggregatesWithEvents.ForEach(it => EventStore.SaveSingleAggregateEvents(it.Value)));
      var allAggregateIds = EventStore.StreamAggregateIdsInCreationOrder<ISomeEvent>()
                                      .ToList();
      allAggregateIds.Should().HaveCount(aggregatesWithEvents.Count);
   });

   [PluggableComponentsTheory]
   public void Does_not_call_db_in_constructor(PluggableComponentTestContext context) => Init(context).ExecuteInIsolatedScope(() => _serviceLocator.Resolve<IEventStoreUpdater>());

   [PluggableComponentsTheory]
   public void ShouldNotCacheEventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction(PluggableComponentTestContext context) => Init(context).ExecuteInIsolatedScope(() =>
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

   [PluggableComponentsTheory]
   public void ShouldCacheEventsBetweenInstancesTransaction(PluggableComponentTestContext context)
   {
      _serviceLocator = context.CreateServiceLocator();

      var user = new User();
      using(_serviceLocator.BeginScope())
      {
         var eventStore = _serviceLocator.EventStore();

         user.Register("email@email.se", "password", Guid.NewGuid());

         TransactionScopeCe.Execute(() =>
         {
            ((IEventStored)user).Commit(eventStore.SaveSingleAggregateEvents);
            eventStore.GetAggregateHistory(user.Id);
            eventStore.GetAggregateHistory(user.Id).Should().NotBeEmpty();
         });
      }

      var firstRead = _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.EventStore().GetAggregateHistory(user.Id).Single());

      var secondRead = _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.EventStore().GetAggregateHistory(user.Id).Single());

      firstRead.Should().BeSameAs(secondRead);
   }
}
