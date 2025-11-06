using System;
using System.Threading;
using Compze.Utilities.Contracts;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

class ThinMonitorWrapper
{
   static readonly TimeSpan InfiniteTimeOut = -1.Milliseconds();//https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor.tryenter?view=net-9.0
   readonly object _lockObject = new();

   public bool TryTakeLock(TimeSpan timeout)
   {
      Assert.Argument.Is(timeout != InfiniteTimeOut, () => "Infinite timeouts are not supported");

      //if(Monitor.TryEnter(_lockObject)) return true; //This will never block and calling it first improves performance quite a bit.
      return Monitor.TryEnter(_lockObject, timeout);
   }

   public void ReleaseLockAndReacquireItOnPulseOrTimeout(TimeSpan timeout) => Monitor.Wait(_lockObject, timeout);

   public void ReleaseLock() => Monitor.Exit(_lockObject);

   public void NotifyWaitingThreadsAboutUpdates() => Monitor.PulseAll(_lockObject); //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
}
