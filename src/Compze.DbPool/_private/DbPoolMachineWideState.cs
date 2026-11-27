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

   ///<summary>The version of the form the reservations are stored in, part of both the machine-wide state's name and the pool databases' names.<br/>
   /// A pool of an older version can't read the reservations a newer one stores, and replaces what it can't read with no reservations at all.<br/>
   /// So pools of different versions, as in two checkouts of different ages on one machine, must share neither the state nor the databases.<br/>
   /// Raise it whenever <see cref="DbPoolState"/> or <see cref="DbPoolDatabase"/> changes what it stores.</summary>
   internal const string FormatVersion = "v2";

   readonly IInterprocessObject<DbPoolState> _shared;

   internal DbPoolMachineWideState(string uniqueName) =>
      _shared = IInterprocessObject.NewGlobal(
         $"{uniqueName}_{FormatVersion}",
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
