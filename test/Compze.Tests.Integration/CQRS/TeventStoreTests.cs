using Compze.Core.Public;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using System.Transactions;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;

namespace Compze.Tests.Integration.CQRS;

interface ISomeTevent : ITaggregateTevent;

class SomeTevent : TaggregateTevent, ISomeTevent
{
   public SomeTevent(TaggregateId taggregateId, int version) : base(taggregateId)
   {
#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)this).SetTaggregateVersionInternal(version);
      ((IMutableTaggregateTevent)this).SetUtcTimeStampInternal(new DateTime(UtcTimeStamp.Year, UtcTimeStamp.Month, UtcTimeStamp.Day, UtcTimeStamp.Hour, UtcTimeStamp.Minute, UtcTimeStamp.Second, DateTimeKind.Utc));
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
      var taggregateId = new TaggregateId();
      var savedEvents = 1.Through(10)
                         .Select(i => new SomeTevent(taggregateId, i))
                         .ToList();
      TransactionScopeCe.Execute(() =>
      {
         TeventStore.SaveSingleTaggregateTevents(savedEvents);
      });

      TeventStore.ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize().Cast<SomeTevent>()
                 .ToList()
                 .Must()
                 .DeepEqual(savedEvents);
   });

   [PCT]
   public void StreamTeventsSinceReturnsWholeTeventLogWhenFetchingALargeNumberOfTevents_EnsureBatchingDoesNotBreakThings() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      const int batchSize = 100;
      const int moreTeventsThanTheBatchSizeForStreamingTevents = batchSize + 10;
      var taggregateId = new TaggregateId();
      var savedEvents = 1.Through(moreTeventsThanTheBatchSizeForStreamingTevents)
                         .Select(i => new SomeTevent(taggregateId, i)).ToList();

      TransactionScopeCe.Execute(() => TeventStore.SaveSingleTaggregateTevents(savedEvents));

      TeventStore.ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(batchSize: batchSize)
                 .Cast<SomeTevent>()
                 .ToList()
                 .Must()
                 .DeepEqual(savedEvents);
   });

   [PCT]
   public void DeleteTeventsDeletesTheTeventsForOnlyTheSpecifiedTaggregate() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var taggregatesWithTevents = 1.Through(10)
                                    .ToDictionary(i => i,
                                                  _ =>
                                                  {
                                                     var taggregateId = new TaggregateId();
                                                     return 1.Through(10)
                                                             .Select(j => new SomeTevent(taggregateId, j))
                                                             .ToList();
                                                  });

      TransactionScopeCe.Execute(() => taggregatesWithTevents.ForEach(it => TeventStore.SaveSingleTaggregateTevents(it.Value)));
      var toRemove = taggregatesWithTevents[2][0].TaggregateId;
      taggregatesWithTevents.Remove(2);

      TransactionScopeCe.Execute(() => TeventStore.DeleteTaggregate(toRemove));

      taggregatesWithTevents.Select(kvp => TeventStore.GetTaggregateHistory(kvp.Value[0].TaggregateId))
                            .ForEach(stream => stream.Must().HaveCount(10));

      TeventStore.GetTaggregateHistory(toRemove)
                 .Must()
                 .BeEmpty();
   });

   [PCT]
   public void GetListOfTaggregateIds() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var taggregateIds = 1.Through(10).Select(_ => new TaggregateId()).ToList();
      var taggregatesWithTevents = taggregateIds
                                    .ToDictionary(i => i,
                                                  taggregateId =>
                                                  {
                                                     return 1.Through(10)
                                                             .Select(j => new SomeTevent(taggregateId, j))
                                                             .ToList();
                                                  });

      TransactionScopeCe.Execute(() => taggregatesWithTevents.ForEach(it => TeventStore.SaveSingleTaggregateTevents(it.Value)));

      var allTaggregateIds = TeventStore.StreamTaggregateIdsInCreationOrder()
                                        .ToList();
      allTaggregateIds.Must().DeepEqual(taggregateIds);
   });

   //Todo: This does not check that only taggregates of the correct type are returned since there are only tevents of type SomeTevent in the store..
   [PCT]
   public void GetListOfTaggregateIdsUsingTeventType() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var taggregateIds = 1.Through(10).Select(_ => new TaggregateId()).ToList();

      var taggregatesWithTevents = taggregateIds
                                    .ToDictionary(i => i,
                                                  taggregateId =>
                                                  {
                                                     return 1.Through(10)
                                                             .Select(j => new SomeTevent(taggregateId, j))
                                                             .ToList();
                                                  });

      TransactionScopeCe.Execute(() => taggregatesWithTevents.ForEach(it => TeventStore.SaveSingleTaggregateTevents(it.Value)));
      var allTaggregateIds = TeventStore.StreamTaggregateIdsInCreationOrder<ISomeTevent>()
                                        .ToList();

      allTaggregateIds.Must().DeepEqual(taggregateIds);
   });

   [PCT]
   public void Does_not_call_db_in_constructor() => _serviceLocator.ExecuteInIsolatedScope(() => _serviceLocator.Resolve<ITeventStoreUpdater>());

   [PCT]
   public void ShouldNotCacheTeventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction() => _serviceLocator.ExecuteInIsolatedScope(() =>
   {
      var teventStore = _serviceLocator.TeventStore();

      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());

      using(new TransactionScope())
      {
         ((ITaggregate)user).Commit(teventStore.SaveSingleTaggregateTevents);
         teventStore.GetTaggregateHistory(user.Id);
         teventStore.GetTaggregateHistory(user.Id).Must().NotBeEmpty();
      }

      teventStore.GetTaggregateHistory(user.Id).Must().BeEmpty();
   });

   [PCT]
   public void ShouldCacheTeventsBetweenInstancesTransaction()
   {
      using var serviceLocator = TestEnv.DIContainer.SetupTestingServiceLocator();

      var user = new User();
      using(serviceLocator.BeginScope())
      {
         var teventStore = serviceLocator.TeventStore();

         user.Register("email@email.se", "password", new TaggregateId());

         TransactionScopeCe.Execute(() =>
         {
            ((ITaggregate)user).Commit(teventStore.SaveSingleTaggregateTevents);
            teventStore.GetTaggregateHistory(user.Id);
            teventStore.GetTaggregateHistory(user.Id).Must().NotBeEmpty();
         });
      }

      var firstRead = serviceLocator.ExecuteInIsolatedScope(() => serviceLocator.TeventStore().GetTaggregateHistory(user.Id).Single());

      var secondRead = serviceLocator.ExecuteInIsolatedScope(() => serviceLocator.TeventStore().GetTaggregateHistory(user.Id).Single());

      firstRead.Must().ReferenceEqual(secondRead);
   }
}
