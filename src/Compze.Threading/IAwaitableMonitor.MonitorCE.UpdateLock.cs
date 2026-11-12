namespace Compze.Threading;

public partial interface IAwaitableMonitor
{
   partial class MonitorCE
   {
      ///<summary>The <see cref="IUpdateLock"/> a <see cref="MonitorCE"/> hands out: disposing it wakes the threads waiting for a condition, so they reevaluate it, and releases the lock.<br/>
      /// A monitor creates its one instance when it is created and hands out that instance every time, so taking an update lock allocates nothing.</summary>
      sealed class UpdateLock(MonitorCE monitor) : IUpdateLock
      {
         readonly MonitorCE _monitor = monitor;

         public void Dispose() => _monitor.ReleaseUpdateLock();
      }
   }
}
