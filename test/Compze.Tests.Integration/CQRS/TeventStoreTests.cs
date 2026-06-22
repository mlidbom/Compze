using Compze.Abstractions.Public;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using System.Transactions;
using Compze.Tessaging.Teventive.TeventStore.Public;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;

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
   readonly IDependencyInjectionContainer _container = TestEnv.DIContainer.SetupTestingContainer(mapper => mapper.RegisterIntegrationTestTypeMappings());

   protected override void DisposeInternal() => _container.Dispose();

   [PCT]
   public void StreamTeventsSinceReturnsWholeTeventLogWhenFromTeventIdIsNull() => _container.ExecuteInIsolatedScope(scope =>
   {
      var taggregateId = new TaggregateId();
      var savedEvents = 1.Through(10)
                         .Select(i => new SomeTevent(taggregateId, i))
                         .ToList();
      TransactionScopeCe.Execute(() =>
      {
         scope.TeventStore().SaveSingleTaggregateTevents(savedEvents);
      });

      scope.TeventStore().ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize().Cast<SomeTevent>()
                 .ToList()
                 .Must()
                 .DeepEqual(savedEvents);
   });

   [PCT]
   public void StreamTeventsSinceReturnsWholeTeventLogWhenFetchingALargeNumberOfTevents_EnsureBatchingDoesNotBreakThings() => _container.ExecuteInIsolatedScope(scope =>
   {
      const int batchSize = 100;
      const int moreTeventsThanTheBatchSizeForStreamingTevents = batchSize + 10;
      var taggregateId = new TaggregateId();
      var savedEvents = 1.Through(moreTeventsThanTheBatchSizeForStreamingTevents)
                         .Select(i => new SomeTevent(taggregateId, i)).ToList();

      TransactionScopeCe.Execute(() => scope.TeventStore().SaveSingleTaggregateTevents(savedEvents));

      scope.TeventStore().ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(batchSize: batchSize)
                 .Cast<SomeTevent>()
                 .ToList()
                 .Must()
                 .DeepEqual(savedEvents);
   });

   [PCT]
   public void DeleteTeventsDeletesTheTeventsForOnlyTheSpecifiedTaggregate() => _container.ExecuteInIsolatedScope(scope =>
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

      TransactionScopeCe.Execute(() => taggregatesWithTevents.ForEach(it => scope.TeventStore().SaveSingleTaggregateTevents(it.Value)));
      var toRemove = taggregatesWithTevents[2][0].TaggregateId;
      taggregatesWithTevents.Remove(2);

      TransactionScopeCe.Execute(() => scope.TeventStore().DeleteTaggregate(toRemove));

      taggregatesWithTevents.Select(kvp => scope.TeventStore().GetTaggregateHistory(kvp.Value[0].TaggregateId))
                            .ForEach(stream => stream.Must().HaveCount(10));

      scope.TeventStore().GetTaggregateHistory(toRemove)
                 .Must()
                 .BeEmpty();
   });

   [PCT]
   public void GetListOfTaggregateIds() => _container.ExecuteInIsolatedScope(scope =>
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

      TransactionScopeCe.Execute(() => taggregatesWithTevents.ForEach(it => scope.TeventStore().SaveSingleTaggregateTevents(it.Value)));

      var allTaggregateIds = scope.TeventStore().StreamTaggregateIdsInCreationOrder()
                                        .ToList();
      allTaggregateIds.Must().DeepEqual(taggregateIds);
   });

   //Todo: This does not check that only taggregates of the correct type are returned since there are only tevents of type SomeTevent in the store..
   [PCT]
   public void GetListOfTaggregateIdsUsingTeventType() => _container.ExecuteInIsolatedScope(scope =>
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

      TransactionScopeCe.Execute(() => taggregatesWithTevents.ForEach(it => scope.TeventStore().SaveSingleTaggregateTevents(it.Value)));
      var allTaggregateIds = scope.TeventStore().StreamTaggregateIdsInCreationOrder<ISomeTevent>()
                                        .ToList();

      allTaggregateIds.Must().DeepEqual(taggregateIds);
   });

   [PCT]
   public void Does_not_call_db_in_constructor() => _container.ExecuteInIsolatedScope(scope => scope.TeventStoreUpdater());

   [PCT]
   public void ShouldNotCacheTeventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction() => _container.ExecuteInIsolatedScope(scope =>
   {
      var teventStore = scope.TeventStore();

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
      using var container = TestEnv.DIContainer.SetupTestingContainer(mapper => mapper.RegisterIntegrationTestTypeMappings());

      var user = new User();
      {
         using var scope = container.BeginScope();
         var teventStore = scope.Resolver.TeventStore();

         user.Register("email@email.se", "password", new TaggregateId());

         TransactionScopeCe.Execute(() =>
         {
            ((ITaggregate)user).Commit(teventStore.SaveSingleTaggregateTevents);
            teventStore.GetTaggregateHistory(user.Id);
            teventStore.GetTaggregateHistory(user.Id).Must().NotBeEmpty();
         });
      }

      var firstRead = container.ExecuteInIsolatedScope(scope => scope.TeventStore().GetTaggregateHistory(user.Id).Single());

      var secondRead = container.ExecuteInIsolatedScope(scope => scope.TeventStore().GetTaggregateHistory(user.Id).Single());

      firstRead.Must().ReferenceEqual(secondRead);
   }
}
