using Compze.DependencyInjection;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.DbPool.Tests;

//Temporary experiment, not to be committed: shows a pool's database reset by another pool once the first pool has held it past the reservation length.
public class Experiment_a_database_held_past_the_reservation_length : DbPoolTestBase
{
   [PCT] public void is_reset_by_the_next_reservation_of_another_pool()
   {
      var reservationName = $"experiment-{Guid.NewGuid()}";
      UseConnection(reservationName, Pool, connection =>
      {
         using var command = connection.CreateCommand();
         command.CommandText = "create table survives_the_reservations_of_other_pools (id int)";
         command.ExecuteNonQuery();
      });
      Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff}Z held: {Pool.ConnectionStringFor(reservationName)}");

      Thread.Sleep(TimeSpan.FromSeconds(70));

      using var otherContainer = CreateContainer();
      var otherPool = otherContainer.Resolve<DbPool>();
      Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff}Z other pool got: {otherPool.ConnectionStringFor($"experiment-other-{Guid.NewGuid()}")}");

      UseConnection(reservationName, Pool, connection =>
      {
         using var command = connection.CreateCommand();
         command.CommandText = "select count(*) from survives_the_reservations_of_other_pools";
         command.ExecuteScalar();
      });
   }
}
