using System;
using System.Threading;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

class ThinMonitorWrapper
{
   readonly object _lockObject = new();

   public bool TryTakeLock(TimeSpan timeout)
   {
      if(Monitor.TryEnter(_lockObject)) return true; //This will never block and calling it first improves performance quite a bit.

      var lockTaken = false;
      try
      {
         Monitor.TryEnter(_lockObject, timeout, ref lockTaken);
         return lockTaken;
      }
      catch(Exception) //It is rare, but apparently possible, for TryEnter to throw an exception after the lock is taken. So we need to catch it and call Monitor.Exit if that happens to avoid leaking locks.
      {
         if(lockTaken) Monitor.Exit(_lockObject);
         throw;
      }
   }

   public void ReleaseLockAndReacquireItOnPulseOrTimeout(TimeSpan timeout) => Monitor.Wait(_lockObject, timeout);

   public void ReleaseLock() => Monitor.Exit(_lockObject);

   public void NotifyWaitingThreadsAboutUpdates() => Monitor.PulseAll(_lockObject); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
}
