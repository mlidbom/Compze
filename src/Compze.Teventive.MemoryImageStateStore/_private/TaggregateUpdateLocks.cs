using Compze.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive.MemoryImageStateStore._private;

///<summary>The lock of each taggregate that a unit of work is changing, which makes a second unit of work that gets it for update<br/>
/// wait for the first to complete. Only the locks held or waited for are kept, not one for each taggregate of the store.</summary>
///<remarks>A lock is held from a unit of work's first get for update until its transaction completes, possibly on another thread,<br/>
/// so it is a <see cref="SemaphoreSlim"/>, the tool the locking standard gives that job.</remarks>
sealed class TaggregateUpdateLocks
{
   //todo: (Magnus, 2026-11-17) replace the SemaphoreSlim with a Compze.Threading lock owned by a transaction, whose timeout
   //   names the transaction holding it and where it took the lock, as the other Compze.Threading locks' timeouts do. An effort
   //   of its own after the spike; until then the 30-second wait and its TimeoutException naming the taggregate stay as they are.

   static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

   readonly IThreadShared<Dictionary<TaggregateId, UpdateLock>> _locksHeldOrWaitedFor = IThreadShared.New(new Dictionary<TaggregateId, UpdateLock>());

   ///<summary>Takes the lock of the taggregate <paramref name="id"/>, waiting while another unit of work holds it.</summary>
   internal UpdateLock Take(TaggregateId id)
   {
      var updateLock = Use(id);
      if(!updateLock.Semaphore.Wait(Timeout)) LeaveAndThrowTheTimeout(updateLock);
      return updateLock;
   }

   ///<summary>As <see cref="Take"/>, holding no thread while another unit of work holds the lock.</summary>
   internal async Task<UpdateLock> TakeAsync(TaggregateId id)
   {
      var updateLock = Use(id);
      if(!await updateLock.Semaphore.WaitAsync(Timeout).caf()) LeaveAndThrowTheTimeout(updateLock);
      return updateLock;
   }

   ///<summary>The lock of the taggregate <paramref name="id"/>, counting the caller among its users, who holds it or waits for it.</summary>
   UpdateLock Use(TaggregateId id) => _locksHeldOrWaitedFor.Locked(locks =>
   {
      if(!locks.TryGetValue(id, out var existing)) locks[id] = existing = new UpdateLock(this, id);
      existing.Users++;
      return existing;
   });

   void LeaveAndThrowTheTimeout(UpdateLock updateLock)
   {
      Leave(updateLock);
      throw new TimeoutException($"Waited {Timeout} for another unit of work to finish changing taggregate {updateLock.Id}.");
   }

   ///<summary>Whether a unit of work holds, or waits for, the lock of the taggregate <paramref name="id"/>.</summary>
   internal bool IsHeldOrWaitedFor(TaggregateId id) => _locksHeldOrWaitedFor.Locked(locks => locks.ContainsKey(id));

   void Leave(UpdateLock updateLock) => _locksHeldOrWaitedFor.Locked(locks =>
   {
      if(--updateLock.Users > 0) return;
      locks.Remove(updateLock.Id);
      updateLock.Semaphore.Dispose();
   });

   ///<summary>The lock of one taggregate, kept while a unit of work holds it or waits for it.</summary>
   internal sealed class UpdateLock(TaggregateUpdateLocks locks, TaggregateId id)
   {
      readonly TaggregateUpdateLocks _locks = locks;
      internal TaggregateId Id { get; } = id;
      internal SemaphoreSlim Semaphore { get; } = new(1, 1);

      ///<summary>The units of work holding or waiting for this lock. Read and written only under the lock of the table of locks.</summary>
      internal int Users { get; set; }

      internal void Release()
      {
         Semaphore.Release();
         _locks.Leave(this);
      }
   }
}
