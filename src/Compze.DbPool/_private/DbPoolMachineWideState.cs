using Compze.DbPool._private.MachineWideState;
using Compze.Threading.Interprocess.ResourceAccess;
using Compze.Internals.SystemCE;
using Compze.Internals.SystemCE.IOCE;
using Compze.Threading;

namespace Compze.DbPool._private;

///<summary>Which of the <see cref="DbPool"/>'s databases are reserved, and by whom,<br/>
/// kept in an <see cref="IInterprocessObject{T}"/> that every process on this machine shares.</summary>
class DbPoolMachineWideState : IDisposable
{
   //todo: (Magnus, 2026-11-17) This class is to be replaced by a pool service that every machine shares: machines that use
   //   the same database servers reset each other's databases today. See
   //   src/Compze.DbPool/dev_docs/TODO/a-pool-service-shared-across-machines.md.

   static readonly Lazy<DirectoryCE> DataDirectory = new(() => DirectoryCE.StandardDirectories
                                                                          .LocalApplicationData
                                                                          .GetOrCreateDirectory("Compze")
                                                                          .GetOrCreateDirectory("SharedFiles"));

   ///<summary>The version of the form the reservations are stored in, part of both the machine-wide state's name and the pool databases' names.<br/>
   /// A pool of an older version can't read the reservations a newer one stores, and replaces what it can't read with no reservations at all,<br/>
   /// nor can it write a state larger than its own <see cref="BytesForEachDatabase"/> allowed for.<br/>
   /// So pools of different versions, as in two checkouts of different ages on one machine, must share neither the state nor the databases.<br/>
   /// Raise it whenever <see cref="DbPoolState"/> or <see cref="DbPoolDatabase"/> changes what it stores, or the room for it shrinks or grows.</summary>
   internal const string FormatVersion = "v3";

   ///<summary>The room in the state for each of the databases <see cref="IDbPoolSqlLayer.MaximumNumberOfDatabases"/> allows. A reserved database<br/>
   /// takes about 75 bytes and its reservation name in UTF-8, up to three bytes for each character, so this leaves room for names of up to<br/>
   /// <see cref="LongestReservationNameTheStateHasRoomFor"/> characters of any kind.</summary>
   internal const int BytesForEachDatabase = 1024;

   ///<summary>The longest reservation names the state has room for when every database is reserved: see <see cref="BytesForEachDatabase"/>. A<br/>
   /// state that grows past its room throws at the reservation that makes it do so.</summary>
   internal const int LongestReservationNameTheStateHasRoomFor = 300;

   ///<summary>How long <see cref="ReserveDatabase"/> waits for a free database before it gives up.</summary>
   internal static readonly TimeSpan PatienceWithNoFreeDatabase = 45.Seconds();

   readonly IInterprocessObject<DbPoolState> _shared;
   readonly int _maximumNumberOfDatabases;

   internal DbPoolMachineWideState(string uniqueName, int maximumNumberOfDatabases)
      : this(uniqueName, maximumNumberOfDatabases, DataDirectory.Value.GetDirectoryInfo()) {}

   ///<summary>A state kept in <paramref name="directory"/> instead of the folder that every process on this machine shares its states in.</summary>
   internal DbPoolMachineWideState(string uniqueName, int maximumNumberOfDatabases, DirectoryInfo directory)
   {
      _maximumNumberOfDatabases = maximumNumberOfDatabases;
      _shared = IInterprocessObject.NewGlobal(
         $"{uniqueName}_{FormatVersion}",
         MemoryPackDbPoolStateSerializer.Instance,
         () => new DbPoolState(),
         CorruptionAction.ReplaceContentWithDefaultAndThrow,
         maxBytes: maximumNumberOfDatabases * BytesForEachDatabase,
         directory);
   }

   internal DbPoolDatabase ReserveDatabase(string reservationName, Guid poolId, TimeSpan reservationLength)
   {
      DbPoolDatabase? reserved = null;

      var overallDeadline = DateTime.UtcNow + PatienceWithNoFreeDatabase;
      while(true)
      {
         var timeUntilNextLeaseExpiration = _shared.Read(state =>
         {
            var expiration = state.EarliestReservationExpiration;
            return expiration > DateTime.UtcNow ? expiration - DateTime.UtcNow : TimeSpan.Zero;
         });

         var remainingTime = overallDeadline - DateTime.UtcNow;
         if(remainingTime <= TimeSpan.Zero)
            throw _shared.Read(state => new TimedOutWaitingForAFreeDatabaseException(state, _maximumNumberOfDatabases, PatienceWithNoFreeDatabase));

         var waitTimeout = new WaitTimeout(remainingTime < timeUntilNextLeaseExpiration ? remainingTime : timeUntilNextLeaseExpiration);

         if(_shared.TryUpdateWhen(state => state.TryReserve(reservationName, poolId, reservationLength, _maximumNumberOfDatabases, out reserved),_ => {}, timeout: waitTimeout))
         {
            return reserved!;
         }
      }
   }

   internal void ReleaseReservationsFor(Guid poolId) => _shared.Update(state => state.ReleaseReservationsFor(poolId));

   ///<summary>Releases this instance's hold on the shared state: its mapping of the file and its handles to the lock. The reservations stay.</summary>
   public void Dispose() => _shared.Dispose();
}
