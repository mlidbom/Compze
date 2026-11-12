namespace Compze.Threading;

public partial interface IAwaitableMonitor
{
   partial class MonitorCE
   {
      ///<summary>The <see cref="IReadLock"/>, and the <see cref="ILock"/>, a <see cref="MonitorCE"/> hands out: disposing it releases the lock.<br/>
      /// A monitor creates its one instance when it is created and hands out that instance every time, so taking a lock allocates nothing.</summary>
      sealed class ReadLock(MonitorCE monitor) : ILock, IReadLock
      {
         readonly MonitorCE _monitor = monitor;

         public void Dispose() => _monitor.ReleaseLock();
      }
   }
}
