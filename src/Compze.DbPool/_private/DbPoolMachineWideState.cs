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

         if(_shared.TryUpdateWhen(state => state.TryReserve(reservationName, poolId, reservationLength, out reserved),_ => {}, timeout: waitTimeout))
         {
            return reserved!;
         }
      }
   }

   internal void ReleaseReservationsFor(Guid poolId) => _shared.Update(state => state.ReleaseReservationsFor(poolId));
}
