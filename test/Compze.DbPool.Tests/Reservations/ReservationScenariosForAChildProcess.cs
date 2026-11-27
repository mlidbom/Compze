using Compze.DependencyInjection;
using Compze.Tests.Infrastructure.XUnit;
using Compze.xUnitBDD;
using Xunit;

// ReSharper disable InconsistentNaming

namespace Compze.DbPool.Tests.Reservations;

///<summary>What the specifications of the <see cref="DbPool"/>'s reservations have another process do, through <see cref="ExplicitTestInAChildProcess"/>.<br/>
/// Each is explicit, so that no ordinary test run runs it.</summary>
public class ReservationScenariosForAChildProcess
{
   ///<summary>The file that <see cref="reserve_a_database_and_hold_it_until_killed"/> writes the name of the database it reserved to.</summary>
   internal const string DatabaseFileVariable = "COMPZE_DBPOOL_SPECIFICATIONS_DATABASE_FILE";

   ///<summary>Reserves a database on <see cref="After_a_process_holding_a_reservation_is_killed.SqlLayer"/>, writes its name to the file<br/>
   /// <see cref="DatabaseFileVariable"/> names, and holds it until the process is killed, which it would be long before the end of its wait.</summary>
   [XF(Explicit = true)] public async Task reserve_a_database_and_hold_it_until_killed()
   {
      await using var container = DatabaselessSqlLayer.ContainerWithAPoolOn<After_a_process_holding_a_reservation_is_killed.SqlLayer>();
      var database = container.Resolve<DbPool>().ConnectionStringFor("held until its process is killed");

      var databaseFile = Environment.GetEnvironmentVariable(DatabaseFileVariable)
                      ?? throw new InvalidOperationException($"The environment variable {DatabaseFileVariable} isn't set: only a specification of the DbPool's reservations runs this scenario, in a process of its own.");
      //Written beside the file and then moved into place, so that the specification never reads a name half written.
      await File.WriteAllTextAsync($"{databaseFile}.writing", database, TestContext.Current.CancellationToken);
      File.Move($"{databaseFile}.writing", databaseFile);

      await Task.Delay(TimeSpan.FromMinutes(5), TestContext.Current.CancellationToken);
   }
}
