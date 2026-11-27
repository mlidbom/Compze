using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TeventStore.Abstractions;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.TeventStore;

///<summary>A tevent store holding a user, which more units of work at once get for update than the database provider's connection pool<br/>
/// has connections: twice the ten a test database's pool has.</summary>
///<remarks>A unit of work that waits for the user's lock holds a connection of the pool, its transaction's, until it completes. So with<br/>
/// as many waiting as the pool has connections, the unit of work holding the lock finds none free, and completes only if it needs no<br/>
/// connection but its transaction's.<br/>
/// Twice, not more: the units of work get the lock one at a time, and those queued for a connection behind them time out after the<br/>
/// provider's connection timeout, fifteen seconds by default, as 128 did on MySQL.</remarks>
public class Given_a_tevent_store_holding_a_user : UniversalTestBase
{
   const int UnitsOfWork = 20;

   readonly IDependencyInjectionContainer _container;
   readonly TaggregateId _userId = TaggregateId.New();

   protected Given_a_tevent_store_holding_a_user()
   {
      _container = TestEnv.DIContainer.SetupTestingContainer(registrar => registrar.RequireIntegrationTestTypeMappings(),
                                                             declareTeventStoreTaggregates: store => store.Taggregate<User>());
      InUnitOfWork(taggregates => User.Register(taggregates, "registered@example.com", "password", _userId));
   }

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync().AsTask();

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) => _container.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.TaggregateStoreUpdater()));

   int EmailChangesInTheUsersHistory => _container.RootResolver.Resolve<ITeventStoreReader>().GetHistory(_userId).Count(it => it.Tevent is IUserChangedEmail);

   ///<summary>Each of <see cref="UnitsOfWork"/> units of work, on a thread of its own, gets the user for update and changes its email. A thread<br/>
   /// of its own, as each waits for the user's lock blocking its thread: on thread-pool threads, they would starve the pool.</summary>
   async Task EachGetTheUserForUpdateAndChangeItsEmailAtOnce() =>
      await Task.WhenAll(Enumerable.Range(0, UnitsOfWork)
                                   .Select(number => TaskCE.RunOnDedicatedThreadAsync(
                                              () => InUnitOfWork(taggregates => taggregates.GetForUpdate<User>(_userId).ChangeEmail($"user{number}@example.com")))));

   ///<summary>Each of <see cref="UnitsOfWork"/> units of work, started on a thread-pool thread, as an ASP.NET Core route's is, gets the user for<br/>
   /// update asynchronously, awaits while it holds it, and changes its email.</summary>
   async Task EachGetTheUserForUpdateAsynchronouslyAndChangeItsEmailAfterAnAwaitAtOnce() =>
      await Task.WhenAll(Enumerable.Range(0, UnitsOfWork)
                                   .Select(number => Task.Run(() => _container.ScopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork =>
                                    {
                                       var user = await unitOfWork.TaggregateStoreUpdater().GetForUpdateAsync<User>(_userId);
                                       await Task.Yield();
                                       user.ChangeEmail($"user{number}@example.com");
                                    }))));

   public class and_more_units_of_work_at_once_than_the_connection_pool_has_connections_each_get_it_for_update_and_change_its_email : Given_a_tevent_store_holding_a_user
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await EachGetTheUserForUpdateAndChangeItsEmailAtOnce();
      }

      [PCT] public void the_users_history_has_an_email_change_from_each_unit_of_work() => EmailChangesInTheUsersHistory.Must().Be(UnitsOfWork);
   }

   public class and_more_units_of_work_at_once_than_the_connection_pool_has_connections_each_get_it_for_update_asynchronously_and_change_its_email_after_an_await : Given_a_tevent_store_holding_a_user
   {
      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         await EachGetTheUserForUpdateAsynchronouslyAndChangeItsEmailAfterAnAwaitAtOnce();
      }

      [PCT] public void the_users_history_has_an_email_change_from_each_unit_of_work() => EmailChangesInTheUsersHistory.Must().Be(UnitsOfWork);
   }

   ///<summary>The user's email has changed once, so the units of work below store no type of tevent that the store hasn't stored before.</summary>
   public class whose_email_a_unit_of_work_has_changed : Given_a_tevent_store_holding_a_user
   {
      protected whose_email_a_unit_of_work_has_changed() => InUnitOfWork(taggregates => taggregates.GetForUpdate<User>(_userId).ChangeEmail("changed@example.com"));

      public class and_more_units_of_work_at_once_than_the_connection_pool_has_connections_each_get_it_for_update_and_change_its_email : whose_email_a_unit_of_work_has_changed
      {
         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            await EachGetTheUserForUpdateAndChangeItsEmailAtOnce();
         }

         [PCT] public void the_users_history_has_an_email_change_from_each_unit_of_work_and_the_first_change() => EmailChangesInTheUsersHistory.Must().Be(UnitsOfWork + 1);
      }

      public class and_more_units_of_work_at_once_than_the_connection_pool_has_connections_each_get_it_for_update_asynchronously_and_change_its_email_after_an_await : whose_email_a_unit_of_work_has_changed
      {
         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            await EachGetTheUserForUpdateAsynchronouslyAndChangeItsEmailAfterAnAwaitAtOnce();
         }

         [PCT] public void the_users_history_has_an_email_change_from_each_unit_of_work_and_the_first_change() => EmailChangesInTheUsersHistory.Must().Be(UnitsOfWork + 1);
      }
   }
}
