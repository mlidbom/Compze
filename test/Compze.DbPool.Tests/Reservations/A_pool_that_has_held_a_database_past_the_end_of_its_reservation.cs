using Compze.DbPool.Sqlite;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;
using Xunit;
using static Compze.Must.MustActions;

namespace Compze.DbPool.Tests.Reservations;

///<remarks>On one SQL layer and one container, not on every component combination: the reservation is the pool's own, whatever it runs on, and the<br/>
/// wait past its end would otherwise come once for each combination.</remarks>
public class A_pool_that_has_held_a_database_past_the_end_of_its_reservation : UniversalTestBase
{
   //The reservation length of the pool DbPoolRegistrar.DbPool registers when no debugger is attached. Under a debugger it is 10 minutes, and this
   //specification fails.
   static readonly TimeSpan ReservationLength = TimeSpan.FromSeconds(65);
   const string ReservationName = "held past the end of its reservation";

   readonly IDependencyInjectionContainer _container = DIContainer.Microsoft.CreateTestingContainerBuilder()
                                                                  ._mutate(it => it.Registrar.DbPool().SqliteMemoryDbPoolSqlLayer())
                                                                  .Build();
   readonly DbPool _pool;

   public A_pool_that_has_held_a_database_past_the_end_of_its_reservation()
   {
      _pool = _container.Resolve<DbPool>();
      _pool.ConnectionStringFor(ReservationName);
   }

   protected override async Task InitializeAsyncInternal() => await Task.Delay(ReservationLength + TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

   [XF] public void throws_an_exception_naming_the_reservation_when_disposed() =>
      Invoking(_pool.Dispose).Must().Throw<Exception>().Which.Message.Must().Contain(ReservationName);

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();
}
