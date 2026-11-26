using Compze.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE;
using System.Transactions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions;
using Compze.Abstractions.Time;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations.Tevents;

namespace Compze.Tests.Integration.CQRS;

public class TeventStoreTests : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container = TestEnv.DIContainer.SetupTestingContainer(registrar => registrar.RequireIntegrationTestTypeMappings());

   protected override void DisposeInternal() => _container.Dispose();

   static IReadOnlyList<ITaggregateTevent<ITaggregateTeventWithIdentity>> WithIdentity(IReadOnlyList<ITaggregateTevent<ITaggregateTevent>> committedTevents) =>
      [.. committedTevents.Cast<ITaggregateTevent<ITaggregateTeventWithIdentity>>()];

   ///<summary>A new <see cref="TestTaggregate"/> that has published <paramref name="teventCount"/> tevents: the <see cref="Ec1"/> that creates it, then<br/>
   /// <see cref="E1"/>s. Published with the time frozen, so that their time stamps have the microsecond precision that every supported database keeps.</summary>
   static TestTaggregate ATaggregateThatPublished(int teventCount) =>
      UtcTimeSource.Test.FrozenAtUtcNow().Run(() => TestTaggregate.FromTevents(null, [typeof(Ec1), .. Enumerable.Repeat(typeof(E1), teventCount - 1)]));

   static void SaveTheTeventsOf(ITaggregate taggregate, ITeventStore teventStore) =>
      taggregate.Commit(committed => teventStore.SaveSingleTaggregateTevents(WithIdentity(committed)));

   [PCT]
   public void StreamTeventsSinceReturnsWholeTeventLogWhenFromTeventIdIsNull() => _container.ExecuteInIsolatedScope(scope =>
   {
      var taggregate = ATaggregateThatPublished(10);
      TransactionScopeCe.Execute(() => SaveTheTeventsOf(taggregate, scope.TeventStore()));

      scope.TeventStore().ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize().Tevents()
                 .ToList()
                 .Must()
                 .DeepEqual([.. taggregate.History]);
   });

   [PCT]
   public void StreamTeventsSinceReturnsWholeTeventLogWhenFetchingALargeNumberOfTevents_EnsureBatchingDoesNotBreakThings() => _container.ExecuteInIsolatedScope(scope =>
   {
      const int batchSize = 100;
      const int moreTeventsThanTheBatchSizeForStreamingTevents = batchSize + 10;
      var taggregate = ATaggregateThatPublished(moreTeventsThanTheBatchSizeForStreamingTevents);

      TransactionScopeCe.Execute(() => SaveTheTeventsOf(taggregate, scope.TeventStore()));

      scope.TeventStore().ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize(batchSize: batchSize)
                 .Tevents()
                 .ToList()
                 .Must()
                 .DeepEqual([.. taggregate.History]);
   });

   [PCT]
   public void DeleteTeventsDeletesTheTeventsForOnlyTheSpecifiedTaggregate() => _container.ExecuteInIsolatedScope(scope =>
   {
      var taggregates = 1.Through(10).ToDictionary(i => i, _ => ATaggregateThatPublished(10));

      TransactionScopeCe.Execute(() => taggregates.Values.ForEach(it => SaveTheTeventsOf(it, scope.TeventStore())));
      var toRemove = taggregates[2].Id;
      taggregates.Remove(2);

      TransactionScopeCe.Execute(() => scope.TeventStore().DeleteTaggregate(toRemove));

      taggregates.Values.Select(it => scope.TeventStore().GetTaggregateHistory(it.Id))
                 .ForEach(stream => stream.Must().HaveCount(10));

      scope.TeventStore().GetTaggregateHistory(toRemove)
                 .Must()
                 .BeEmpty();
   });

   [PCT]
   public void GetListOfTaggregateIds() => _container.ExecuteInIsolatedScope(scope =>
   {
      var taggregates = 1.Through(10).Select(_ => ATaggregateThatPublished(10)).ToList();

      TransactionScopeCe.Execute(() => taggregates.ForEach(it => SaveTheTeventsOf(it, scope.TeventStore())));

      var allTaggregateIds = scope.TeventStore().StreamTaggregateIdsInCreationOrder()
                                        .ToList();
      allTaggregateIds.Must().DeepEqual([.. taggregates.Select(it => it.Id)]);
   });

   //todo: This does not check that only taggregates of the correct type are returned, since the store holds only the tevents of TestTaggregates.
   [PCT]
   public void GetListOfTaggregateIdsUsingTeventType() => _container.ExecuteInIsolatedScope(scope =>
   {
      var taggregates = 1.Through(10).Select(_ => ATaggregateThatPublished(10)).ToList();

      TransactionScopeCe.Execute(() => taggregates.ForEach(it => SaveTheTeventsOf(it, scope.TeventStore())));
      var allTaggregateIds = scope.TeventStore().StreamTaggregateIdsInCreationOrder<ITestTaggregateTevent>()
                                        .ToList();

      allTaggregateIds.Must().DeepEqual([.. taggregates.Select(it => it.Id)]);
   });

   [PCT]
   public void Does_not_call_db_in_constructor() => _container.ExecuteInIsolatedScope(scope => scope.TaggregateStoreUpdater());

   [PCT]
   public void ShouldNotCacheTeventsSavedDuringFailedTransactionEvenIfReadDuringSameTransaction() => _container.ExecuteInIsolatedScope(scope =>
   {
      var teventStore = scope.TeventStore();

      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());

      using(new TransactionScope())
      {
         ((ITaggregate)user).Commit(committed => teventStore.SaveSingleTaggregateTevents(WithIdentity(committed)));
         teventStore.GetTaggregateHistory(user.Id);
         teventStore.GetTaggregateHistory(user.Id).Must().NotBeEmpty();
      }

      teventStore.GetTaggregateHistory(user.Id).Must().BeEmpty();
   });

   [PCT]
   public void ShouldCacheTeventsBetweenInstancesTransaction()
   {
      using var container = TestEnv.DIContainer.SetupTestingContainer(registrar => registrar.RequireIntegrationTestTypeMappings());

      var user = new User();
      {
         using var scope = container.BeginScope();
         var teventStore = scope.Resolver.TeventStore();

         user.Register("email@email.se", "password", TaggregateId.New());

         TransactionScopeCe.Execute(() =>
         {
            ((ITaggregate)user).Commit(committed => teventStore.SaveSingleTaggregateTevents(WithIdentity(committed)));
            teventStore.GetTaggregateHistory(user.Id);
            teventStore.GetTaggregateHistory(user.Id).Must().NotBeEmpty();
         });
      }

      var firstRead = container.ExecuteInIsolatedScope(scope => scope.TeventStore().GetTaggregateHistory(user.Id).Single());

      var secondRead = container.ExecuteInIsolatedScope(scope => scope.TeventStore().GetTaggregateHistory(user.Id).Single());

      firstRead.Must().ReferenceEqual(secondRead);
   }
}
