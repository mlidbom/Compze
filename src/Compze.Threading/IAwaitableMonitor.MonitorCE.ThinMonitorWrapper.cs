namespace Compze.Threading;

public partial interface IAwaitableMonitor
{
   partial class MonitorCE
   {
#pragma warning disable CA2002 // Only MonitorCE can reach an instance, so nothing else can lock on it: the case CA2002's documentation names as safe to suppress.
      ///<summary>The .NET <see cref="Monitor"/> a <see cref="MonitorCE"/> is built on, which counts how often taking the lock had to wait.</summary>
      ///<remarks>It locks on itself rather than on an object of its own, which would cost another allocation per monitor.<br/>
      /// That is safe because no code outside <see cref="MonitorCE"/> can reach an instance, so nothing else can lock on it.<br/>
      /// <see cref="MonitorCE"/> itself is handed out to consumers, so it could not lock on itself the same way.</remarks>
      class ThinMonitorWrapper
      {
         long _contentionCount = 0;
         public long ContentionCount => _contentionCount;

         static readonly TimeSpan CancellationPollingInterval = TimeSpan.FromSeconds(1);

         public bool TryTakeLock(LockTimeout timeout, CancellationToken cancellationToken = default)
         {
            if(Monitor.TryEnter(this)) return true; //This will never block, calling it is essentially free and allows us to collect contention statistics
            Interlocked.Increment(ref _contentionCount);
            if(!cancellationToken.CanBeCanceled) return Monitor.TryEnter(this, timeout);

            // Poll with short intervals so we can respond to cancellation
            var deadline = DateTime.UtcNow + timeout.ToTimeSpan();
            while(true)
            {
               cancellationToken.ThrowIfCancellationRequested();
               var remaining = deadline - DateTime.UtcNow;
               if(remaining <= TimeSpan.Zero) return false;
               if(Monitor.TryEnter(this, remaining < CancellationPollingInterval ? remaining : CancellationPollingInterval)) return true;
            }
         }

         public void ReleaseLockAndReacquireItOnPulseOrTimeout(WaitTimeout timeout) => Monitor.Wait(this, timeout);

         public void ReleaseLock() => Monitor.Exit(this);

         public void NotifyWaitingThreadsAboutUpdates() => Monitor.PulseAll(this); //All threads blocked on Monitor.Wait for this wrapper will now try and reacquire the lock

         public void AcquireLockAndNotifyWaitingThreads()
         {
            lock(this) Monitor.PulseAll(this);
         }
      }
#pragma warning restore CA2002
   }
}
