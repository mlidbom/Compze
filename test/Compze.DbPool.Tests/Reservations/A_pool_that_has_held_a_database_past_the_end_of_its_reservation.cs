using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.DbPool.Tests.Reservations;

public class A_pool_that_has_held_a_database_past_the_end_of_its_reservation : UniversalTestBase
{
   //The reservation length of the pool DbPoolRegistrar.DbPool registers when no debugger is attached. Under a debugger it is 10 minutes, and this
   //specification fails.
   static readonly TimeSpan ReservationLength = TimeSpan.FromSeconds(65);

   readonly IDependencyInjectionContainer _container = DatabaselessSqlLayer.ContainerWithAPoolOn<SqlLayer>();
   readonly DbPool _pool;
   readonly string _database;

   public A_pool_that_has_held_a_database_past_the_end_of_its_reservation()
   {
      _pool = _container.Resolve<DbPool>();
      _database = _pool.ConnectionStringFor("held past the end of its reservation");
   }

   protected override async Task InitializeAsyncInternal() => await Task.Delay(ReservationLength + TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

   [XF] public void throws_an_exception_naming_the_database_when_disposed() =>
      Invoking(_pool.Dispose).Must().Throw<Exception>().Which.Message.Must().Contain(_database);

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   sealed class SqlLayer : DatabaselessSqlLayer;
}
