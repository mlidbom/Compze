using Compze.DbPool.MachineWideState;
using Compze.InterprocessObject;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.IOCE;
using Compze.Threading;

namespace Compze.DbPool;

class DbPoolMachineWideState
{
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
         maxCapacityInBytes: 64 * 1024,
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

         if(_shared.TryUpdateWhen(state => state.TryReserve(reservationName, poolId, reservationLength, out reserved),_ => {},waitTimeout))
         {
            return reserved!;
         }
      }
   }

   internal void ReleaseReservationsFor(Guid poolId) => _shared.Update(state => state.ReleaseReservationsFor(poolId));
}
