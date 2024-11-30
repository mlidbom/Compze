using System;
using System.Threading;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess;

///<summary>The monitor class exposes a rather horrifying API in my humble opinion. This class attempts to adapt it to something that is reasonably understandable and less brittle.</summary>
public partial class MonitorCE
{
   internal Lock EnterLock()
   {
      Enter();
      return _lock;
   }

   public NotifyAllLock EnterUpdateLock()
   {
      Enter();
      return _notifyAllLock;
   }

   public sealed class NotifyAllLock : IDisposable
   {
      readonly MonitorCE _monitor;
      internal NotifyAllLock(MonitorCE monitor) => _monitor = monitor;
      public void Dispose()
      {
         Monitor.PulseAll(_monitor._lockObject);   //All threads blocked on Monitor.Wait for our _lockObject will now try and reacquire the lock
         _monitor.Exit();
      }
   }

   internal sealed class Lock : IDisposable
   {
      readonly MonitorCE _monitor;
      internal Lock(MonitorCE monitor) => _monitor = monitor;
      public void Dispose() => _monitor.Exit();
   }
}
