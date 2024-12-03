using System;
using System.Threading;

namespace Compze.SystemCE.ThreadingCE.ResourceAccess;

///<summary>The monitor class exposes a less than inviting and easy to use API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
public partial class MonitorCE
{
   internal ReadLock TakeReadLock()
   {
      Enter();
      return _readLock;
   }

   public UpdateLock TakeUpdateLock() => TakeUpdateLock(_timeout);
   public UpdateLock TakeUpdateLock(TimeSpan timeout)
   {
      Enter(timeout);
      return _updateLock;
   }

   public sealed class UpdateLock : IDisposable
   {
      readonly MonitorCE _monitor;
      internal UpdateLock(MonitorCE monitor) => _monitor = monitor;
      public void Dispose()
      {
         Monitor.PulseAll(_monitor._lockObject);   //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
         _monitor.Exit();
      }
   }

   internal sealed class ReadLock : IDisposable
   {
      readonly MonitorCE _monitor;
      internal ReadLock(MonitorCE monitor) => _monitor = monitor;
      public void Dispose() => _monitor.Exit();
   }
}
