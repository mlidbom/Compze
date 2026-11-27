using Compze.Contracts;

namespace Compze.DbPool;

public partial class DbPool
{
   ///<summary>
   /// The database a <see cref="DbPool"/> holds for one reservation name: reserved from the machine-wide state and reset by
   /// the first <see cref="DbPool.ConnectionStringFor"/> for the name, which does so outside the pool's lock, while the
   /// other calls for the name wait for it to be ready.
   ///</summary>
   ///<remarks>Read and changed only under the pool's lock, except by the call preparing it: while <see cref="IsBeingPrepared"/>, no
   /// other thread reads or changes it.</remarks>
   sealed class Reservation
   {
      ///<summary>The database the machine-wide state reserved for the name: null until a call has reserved one. It stays when its<br/>
      /// reset fails, so that the next call resets it again, and <see cref="DbPool.Dispose"/> releases it.</summary>
      internal DbPoolDatabase? ReservedDatabase { get; private set; }

      ///<summary>Whether a call is reserving or resetting <see cref="ReservedDatabase"/> outside the pool's lock.</summary>
      internal bool IsBeingPrepared { get; private set; }

      ///<summary>Whether <see cref="ReservedDatabase"/> is reserved and reset, ready to be handed out as it is.</summary>
      bool IsReady { get; set; }

      ///<summary>Makes the calling call the one that prepares the reservation, unless it is ready: returns whether it is.</summary>
      internal bool TryBeginPreparation()
      {
         Contract.State.Assert(!IsBeingPrepared);
         if(IsReady) return false;
         IsBeingPrepared = true;
         return true;
      }

      ///<summary>Ends the preparation <see cref="TryBeginPreparation"/> began: <paramref name="database"/> is what it reserved, null when the<br/>
      /// reservation failed, and <paramref name="isReset"/> whether it was reset.</summary>
      internal void EndPreparation(DbPoolDatabase? database, bool isReset)
      {
         Contract.State.Assert(IsBeingPrepared);
         ReservedDatabase = database;
         IsReady = isReset;
         IsBeingPrepared = false;
      }
   }
}
