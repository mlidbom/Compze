using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.xUnitBDD;
using Xunit;

namespace Compze.DbPool.Tests.Reservations;

public class After_a_process_holding_a_reservation_is_killed : UniversalTestBase
{
   static readonly TimeSpan PatienceWithTheOtherProcess = TimeSpan.FromSeconds(60);

   string _databaseTheKilledProcessHeld = null!;
   string _databaseTheNextReservationGot = null!;

   protected override async Task InitializeAsyncInternal()
   {
      _databaseTheKilledProcessHeld = await TheDatabaseAnotherProcessHeldWhenItWasKilledAsync();

      await using var container = DatabaselessSqlLayer.ContainerWithAPoolOn<SqlLayer>();
      _databaseTheNextReservationGot = container.Resolve<DbPool>().ConnectionStringFor("the next reservation");
   }

   [XF] public void the_next_reservation_is_given_the_database_it_held() => _databaseTheNextReservationGot.Must().Be(_databaseTheKilledProcessHeld);

   ///<summary>The SQL layer of this specification's pools and of the other process's, so that no other pool's reservations come between theirs:<br/>
   /// the database the other process held is then the one reserved last, which a reservation is given first once it is released.</summary>
   internal sealed class SqlLayer : DatabaselessSqlLayer;

   static async Task<string> TheDatabaseAnotherProcessHeldWhenItWasKilledAsync()
   {
      var databaseFile = Path.Combine(Path.GetTempPath(), $"compze-dbpool-specification-{Guid.NewGuid()}.database");
      await using var otherProcess = ExplicitTestInAChildProcess.Start(typeof(ReservationScenariosForAChildProcess),
                                                                       nameof(ReservationScenariosForAChildProcess.reserve_a_database_and_hold_it_until_killed),
                                                                       new Dictionary<string, string> { [ReservationScenariosForAChildProcess.DatabaseFileVariable] = databaseFile });
      var waitEndsAt = DateTime.UtcNow + PatienceWithTheOtherProcess;
      while(!File.Exists(databaseFile))
      {
         if(otherProcess.HasExited) throw new InvalidOperationException($"The process that was to hold a database exited:{Environment.NewLine}{await otherProcess.OutputOnceItExitsAsync(PatienceWithTheOtherProcess)}");
         if(DateTime.UtcNow > waitEndsAt) throw new TimeoutException($"The process that was to hold a database didn't reserve one within {PatienceWithTheOtherProcess}:{Environment.NewLine}{await otherProcess.KillAndReadTheOutputAsync()}");
         await Task.Delay(20, TestContext.Current.CancellationToken);
      }

      await otherProcess.KillAsync();
      var database = await File.ReadAllTextAsync(databaseFile, TestContext.Current.CancellationToken);
      File.Delete(databaseFile);
      return database;
   }
}
