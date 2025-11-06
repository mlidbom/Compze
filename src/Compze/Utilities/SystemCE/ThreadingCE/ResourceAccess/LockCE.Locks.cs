using System;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

///<summary>The monitor class exposes a less than inviting and easy to use API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
public partial class LockCE
{
   internal IDisposable TakeReadLock() => TakeReadLock(Timeout);

   public IDisposable TakeUpdateLock() => TakeUpdateLock(Timeout);

   sealed class UpdateLock : IDisposable
   {
      readonly LockCE _monitor;
      internal UpdateLock(LockCE monitor) => _monitor = monitor;

      public void Dispose()
      {
         _monitor._monitor.NotifyWaitingThreadsAboutUpdates(); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
         _monitor.ReleaseLock();
      }
   }

   sealed class ReadLock : IDisposable
   {
      readonly LockCE _monitor;
      internal ReadLock(LockCE monitor) => _monitor = monitor;
      public void Dispose() => _monitor.ReleaseLock();
   }
}
