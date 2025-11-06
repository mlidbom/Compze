using System;
using System.Threading;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

///<summary>The monitor class exposes a less than inviting and easy to use API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
public partial class MonitorCE
{
   internal IDisposable TakeReadLock() => TakeReadLock(Timeout);
   internal IDisposable TakeReadLock(TimeSpan timeout)
   {
      Enter(timeout);
      return _readLock;
   }

   public IDisposable TakeUpdateLock() => TakeUpdateLock(Timeout);
   public IDisposable TakeUpdateLock(TimeSpan timeout)
   {
      Enter(timeout);
      return _updateLock;
   }

   sealed class UpdateLock : IDisposable
   {
      readonly MonitorCE _monitor;
      internal UpdateLock(MonitorCE monitor) => _monitor = monitor;
      public void Dispose()
      {
         Monitor.PulseAll(_monitor._lockObject);   //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
         _monitor.Exit();
      }
   }

   sealed class ReadLock : IDisposable
   {
      readonly MonitorCE _monitor;
      internal ReadLock(MonitorCE monitor) => _monitor = monitor;
      public void Dispose() => _monitor.Exit();
   }
}
