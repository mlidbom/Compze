using Compze.DbPool._private.MachineWideState;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.IOCE;
using Compze.Threading;

namespace Compze.DbPool._private;

///<summary>Which of the <see cref="DbPool"/>'s databases are reserved, and by whom,<br/>
/// kept in an <see cref="IInterprocessObject{T}"/> that every process on this machine shares.</summary>
class DbPoolMachineWideState
{
   //todo: (Magnus, 2026-11-17) This class is to be replaced by a pool service that every machine shares: machines that use
   //   the same database servers reset each other's databases today. See
   //   src/Compze.DbPool/dev_docs/TODO/a-pool-service-shared-across-machines.md.

   static readonly Lazy<DirectoryCE> DataDirectory = new(() => DirectoryCE.StandardDirectories
                                                                          .LocalApplicationData
                                                                          .GetOrCreateDirectory("Compze")
                                                                          .GetOrCreateDirectory("SharedFiles"));

   readonly IInterprocessObject<DbPoolState> _shared;

   internal DbPoolMachineWideState(string uniqueName) =>
      _shared = IInterprocessObject.NewGlobal(
         uniqueName,
         MemoryPackDbPoolStateSerializer.Instance,
         () => new DbPoolState(),
         CorruptionAction.ReplaceContentWithDefaultAndThrow,
         maxBytes: 64 * 1024,
         DataDirectory.Value.GetDirectoryInfo());

   internal DbPoolDatabase ReserveDatabase(string reservationName, Guid poolId, TimeSpan reservationLength)
   {
      DbPoolDatabase? reserved = null;

      var overallDeadline = DateTime.UtcNow + 45.Seconds();
      while(true)
      {
         var timeUntilNextLeaseExpiration = _shared.Read(state =>
         {
            var expiration = state.EarliestReservationExpiration;
            return expiration > DateTime.UtcNow ? expiration - DateTime.UtcNow : TimeSpan.Zero;
         });

         var remainingTime = overallDeadline - DateTime.UtcNow;
         if(remainingTime <= TimeSpan.Zero)
            throw new Exception("Timed out waiting for database. Have you missed disposing a database pool? Please check your logs for errors about non-disposed pools.");

         var waitTimeout = new WaitTimeout(remainingTime < timeUntilNextLeaseExpiration ? remainingTime : timeUntilNextLeaseExpiration);

         List<string> expired = [];
         if(_shared.TryUpdateWhen(state =>
                                  {
                                     expired = state.Databases.Where(db => db.ShouldBeReleased).Select(db => db.ToString()).ToList();
                                     return state.TryReserve(reservationName, poolId, reservationLength, out reserved);
                                  },
                                  _ =>
                                  {
                                     foreach(var it in expired) DiagnosticLog.Write(poolId, $"EXPIRED-RELEASED {it}");
                                     DiagnosticLog.Write(poolId, $"RESERVED {reserved}");
                                  }, timeout: waitTimeout))
         {
            return reserved!;
         }
      }
   }

   internal void ReleaseReservationsFor(Guid poolId, IReadOnlyList<DbPoolDatabase> heldLocally) => _shared.Update(state =>
   {
      foreach(var local in heldLocally)
      {
         var shared = state.Databases.Single(db => db.Id == local.Id);
         DiagnosticLog.Write(poolId, shared.ReservedByPoolId == poolId ? $"RELEASING-OWN {shared}" : $"RELEASING-LOST {local.Name} reservedAt={local.ReservationExpirationTime.AddSeconds(-65):HH:mm:ss.fff}Z now held by: {shared}");
      }
      state.ReleaseReservationsFor(poolId);
   });
}

static class DiagnosticLog
{
   static readonly string Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dbpool-diagnostics.log");
   internal static void Write(Guid poolId, string line)
   {
      for(var attempt = 0; attempt < 50; attempt++)
      {
         try
         {
            File.AppendAllText(Path, $"{DateTime.UtcNow:HH:mm:ss.fff}Z pid={Environment.ProcessId} pool={poolId.ToString()[..8]} {line}{Environment.NewLine}");
            return;
         }
         catch(IOException) { Thread.Sleep(5); }
      }
   }
}
