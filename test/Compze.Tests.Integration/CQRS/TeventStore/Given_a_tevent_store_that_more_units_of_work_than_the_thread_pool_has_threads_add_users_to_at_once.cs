using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.TeventStore;

///<summary>Units of work, each adding a user to a tevent store, started on the thread pool at once: four for each of the pool's threads,<br/>
/// and more than the database providers' pools hold connections by default, a hundred, so that most wait for a thread and for a<br/>
/// connection while the others run.</summary>
///<remarks>Each unit of work holds a connection of the provider's pool through its transaction, and gives it back as the transaction<br/>
/// completes. And each blocks its thread-pool thread while the database answers it: a database layer that needs a free thread-pool<br/>
/// thread to send what it asks, as Npgsql does for a synchronous command of several statements, stalls until the pool adds one.</remarks>
public class Given_a_tevent_store_that_more_units_of_work_than_the_thread_pool_has_threads_add_users_to_at_once : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   readonly TaggregateId[] _userIds;

   public Given_a_tevent_store_that_more_units_of_work_than_the_thread_pool_has_threads_add_users_to_at_once()
   {
      _container = TestEnv.DIContainer.SetupTestingContainer(registrar => registrar.RequireIntegrationTestTypeMappings(),
                                                             declareTeventStoreTaggregates: store => store.Taggregate<User>());
      ThreadPool.GetMinThreads(out var threadsThePoolStartsWith, out _);
      _userIds = [..Enumerable.Range(0, Math.Max(4 * threadsThePoolStartsWith, 128)).Select(_ => TaggregateId.New())];
   }

   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      await Task.WhenAll(_userIds.Select(userId => Task.Run(() => AddAUser(userId))));
   }

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync().AsTask();

   void AddAUser(TaggregateId userId)
   {
      var user = new User();
      user.Register(EmailOf(userId), "password", userId);
      _container.ExecuteUnitOfWork(unitOfWork => unitOfWork.TaggregateStoreUpdater().Add(user));
   }

   static string EmailOf(TaggregateId userId) => $"{userId}@example.com";

   [PCT] public void every_user_is_stored() =>
      _container.ExecuteUnitOfWork(unitOfWork => _userIds.Where(userId => unitOfWork.TaggregateStoreUpdater().GetForUpdate<User>(userId).Email != EmailOf(userId)).ToArray())
                .Must().BeEmpty();
}
