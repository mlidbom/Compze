using System.Data.Common;
using Compze.DependencyInjection;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tests.Infrastructure.XUnit;
using Xunit;

namespace Compze.DbPool.Tests.Reservations;

///<summary>A reset can take seconds on a busy server, or wait for a lock that a test still holds on its database: the pool's calls for<br/>
/// other reservation names must not wait for it. Two endpoints of a host that start together share the host's pool.</summary>
///<remarks>
/// The reset is made to wait as a consumer can make it wait: a first pool reserves a database, a transaction on it holds a lock, and the<br/>
/// first pool releases it. A reservation is given the database released last, so the second pool's first reservation is given that one,<br/>
/// and its reset waits for the transaction. Unless another test process reserves it in between: then the reset waits for no lock, and<br/>
/// the specification is skipped rather than passed.<br/>
/// Not on the Sqlite layers: they name their databases per SQL layer instance, so another pool's database can't be held from outside.
///</remarks>
public class While_a_pools_reset_of_one_database_waits_for_a_lock_on_it : DbPoolTestBase
{
   static readonly TimeSpan LongEnoughForTheResetToBeWaiting = TimeSpan.FromSeconds(2);
   static readonly TimeSpan LongEnoughForAnotherReservationAndReset = TimeSpan.FromSeconds(20);

   [PCT] public async Task a_call_for_another_reservation_name_completes_meanwhile()
   {
      if(TestEnv.SqlLayer is SqlLayer.Sqlite or SqlLayer.SqliteMemory)
         Assert.Skip("The Sqlite layers name their databases per SQL layer instance: no lock from outside a pool can make its reset wait.");

      await using var heldConnection = CreateConnection(await TheConnectionStringOfADatabaseThatAnotherPoolHasReleased());
      await heldConnection.OpenAsync(TestContext.Current.CancellationToken);
      await ExecuteAsync(heldConnection, transaction: null, "CREATE TABLE blocker (id int)");
      await using var transaction = await heldConnection.BeginTransactionAsync(TestContext.Current.CancellationToken);
      await ExecuteAsync(heldConnection, transaction, "INSERT INTO blocker VALUES (1)");

      var waitingReservation = Task.Run(() => Pool.ConnectionStringFor("waits for the lock"), TestContext.Current.CancellationToken);
      await Task.Delay(LongEnoughForTheResetToBeWaiting, TestContext.Current.CancellationToken);
      if(waitingReservation.IsCompleted)
         Assert.Skip("Another test process reserved the released database first, so this pool's reset had no lock to wait for.");

      var otherReservation = Task.Run(() => Pool.ConnectionStringFor("another reservation name"), TestContext.Current.CancellationToken);
      await Task.WhenAny(otherReservation, waitingReservation, Task.Delay(LongEnoughForAnotherReservationAndReset, TestContext.Current.CancellationToken));
      var otherReservationStatus = otherReservation.Status;

      await transaction.RollbackAsync(TestContext.Current.CancellationToken);
      if(DatabaseNamedBy(await waitingReservation) != heldConnection.Database)
         Assert.Skip("Another test process reserved the released database first: this pool's reset waited for a busy server, not for the lock.");

      otherReservationStatus.Must().Be(TaskStatus.RanToCompletion);
   }

   ///<summary>The connection string of a database that a pool reserved and has released: the database the next reservation is given,<br/>
   /// unless another process reserves first.</summary>
   static async Task<string> TheConnectionStringOfADatabaseThatAnotherPoolHasReleased()
   {
      await using var firstContainer = CreateContainer();
      return firstContainer.Resolve<DbPool>().ConnectionStringFor("held by the test");
   }

   static string DatabaseNamedBy(string connectionString)
   {
      using var connection = CreateConnection(connectionString);
      return connection.Database;
   }

   static async Task ExecuteAsync(DbConnection connection, DbTransaction? transaction, string sql)
   {
      await using var command = connection.CreateCommand();
      command.Transaction = transaction;
      command.CommandText = sql;
      await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
   }
}
