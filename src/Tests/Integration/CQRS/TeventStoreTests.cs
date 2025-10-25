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
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
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

public class TeventStoreTests : UniversalTestBase
{
   readonly IServiceLocator _serviceLocator = TestEnv.DIContainer.SetupTestingServiceLocator();
   ITeventStore TeventStore => _serviceLocator.TeventStore();

   protected override void DisposeInternal() => _serviceLocator.Dispose();

   [PCT]
   public void StreamTeventsSinceReturnsWholeTeventLogWhenFromTeventIdIsNull() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var aggregateId = Guid.NewGuid();
      TransactionScopeCe.Execute(() => TeventStore.SaveSingleAggregateTevents(1.Through(10)
                                                                             .Select(i => new SomeTevent(aggregateId, i)).ToList()));
      var stream = TeventStore.ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize();

      stream.Should()
            .HaveCount(10);
   });

   [PCT]
   public void StreamTeventsSinceReturnsWholeTeventLogWhenFetchingALargeNumberOfTevents_EnsureBatchingDoesNotBreakThings() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      const int batchSize = 100;
      const int moreTeventsThanTheBatchSizeForStreamingTevents = batchSize + 10;
      var aggregateId = Guid.NewGuid();

      TransactionScopeCe.Execute(() => TeventStore.SaveSingleAggregateTevents(1.Through(moreTeventsThanTheBatchSizeForStreamingTevents)
                                                                             .Select(i => new SomeTevent(aggregateId, i)).ToList()));

      var stream = TeventStore.ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(batchSize: batchSize)
                             .ToList();

      var currentTeventNumber = 0;
      stream.Should()
            .HaveCount(moreTeventsThanTheBatchSizeForStreamingTevents);
      foreach(var aggregateTevent in stream)
      {
         aggregateTevent.AggregateVersion.Should()
                       .Be(++currentTeventNumber, "Incorrect tevent version detected");
      }
   });

   [PCT]
   public void DeleteTeventsDeletesTheTeventsForOnlyTheSpecifiedAggregate() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithTevents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeTevent(aggregateId, j))
                                                           .ToList();
                                                });

      TransactionScopeCe.Execute(() => aggregatesWithTevents.ForEach(it => TeventStore.SaveSingleAggregateTevents(it.Value)));
      var toRemove = aggregatesWithTevents[2][0]
        .AggregateId;
      aggregatesWithTevents.Remove(2);

      TransactionScopeCe.Execute(() => TeventStore.DeleteAggregate(toRemove));

      aggregatesWithTevents.Select(kvp => TeventStore.GetAggregateHistory(kvp.Value[0].AggregateId))
                          .ForEach(stream => stream.Should().HaveCount(10));

      TeventStore.GetAggregateHistory(toRemove)
                .Should()
                .BeEmpty();
   });

   [PCT]
   public void GetListOfAggregateIds() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithTevents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeTevent(aggregateId, j))
                                                           .ToList();
                                                });

      TransactionScopeCe.Execute(() => aggregatesWithTevents.ForEach(it => TeventStore.SaveSingleAggregateTevents(it.Value)));

      var allAggregateIds = TeventStore.StreamAggregateIdsInCreationOrder()
                                      .ToList();
      allAggregateIds.Should().HaveCount(aggregatesWithTevents.Count);
   });

   //Todo: This does not check that only aggregates of the correct type are returned since there are only tevents of type SomeTevent in the store..
   [PCT]
   public void GetListOfAggregateIdsUsingTeventType() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var aggregatesWithTevents = 1.Through(10)
                                  .ToDictionary(i => i,
                                                _ =>
                                                {
                                                   var aggregateId = Guid.NewGuid();
                                                   return 1.Through(10)
                                                           .Select(j => new SomeTevent(aggregateId, j))
                                                           .ToList();
                                                });

      TransactionScopeCe.Execute(() => aggregatesWithTevents.ForEach(it => TeventStore.SaveSingleAggregateTevents(it.Value)));
      var allAggregateIds = TeventStore.StreamAggregateIdsInCreationOrder<ISomeTevent>()
                                      .ToList();
      allAggregateIds.Should().HaveCount(aggregatesWithTevents.Count);
   });

   [PCT]
   public void Does_not_call_db_in_constructor() => _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.Resolve<ITeventStoreUpdater>());

   [PCT]
   public void ShouldNotCacheTeventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var teventStore = _serviceLocator.TeventStore();

      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

      using(new TransactionScope())
      {
         ((ITeventStored)user).Commit(teventStore.SaveSingleAggregateTevents);
         teventStore.GetAggregateHistory(user.Id);
         teventStore.GetAggregateHistory(user.Id).Should().NotBeEmpty();
      }

      teventStore.GetAggregateHistory(user.Id).Should().BeEmpty();
   });

   [PCT]
   public void ShouldCacheTeventsBetweenInstancesTransaction()
   {
      using var serviceLocator = TestEnv.DIContainer.SetupTestingServiceLocator();

      var user = new User();
      using(serviceLocator.BeginScope())
      {
         var teventStore = serviceLocator.TeventStore();

         user.Register("email@email.se", "password", Guid.NewGuid());

         TransactionScopeCe.Execute(() =>
         {
            ((ITeventStored)user).Commit(teventStore.SaveSingleAggregateTevents);
            teventStore.GetAggregateHistory(user.Id);
            teventStore.GetAggregateHistory(user.Id).Should().NotBeEmpty();
         });
      }

      var firstRead = serviceLocator.ExecuteInIsolatedScope(() => serviceLocator.TeventStore().GetAggregateHistory(user.Id).Single());

      var secondRead = serviceLocator.ExecuteInIsolatedScope(() => serviceLocator.TeventStore().GetAggregateHistory(user.Id).Single());

      firstRead.Should().BeSameAs(secondRead);
   }
}
