using Compze.Contracts;

namespace Compze.Threading;

public partial interface IAwaitableMonitor
{
#pragma warning disable CA1001 //By creating the locks only once in the constructor usages become zero-allocation operations.
   partial class MonitorCE
#pragma warning restore CA1001
   {
      public Task<bool> TryAwaitAsync(Func<bool> condition, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryExecuteWhenAsync(condition, static () => {}, _readLock, waitTimeout, lockTimeout, cancellationToken);

      public Task<bool> TryUpdateWhenAsync(Func<bool> condition, Action action, CancellationToken cancellationToken = default, WaitTimeout? waitTimeout = null, LockTimeout? lockTimeout = null) =>
         TryExecuteWhenAsync(condition, action, _updateLock, waitTimeout, lockTimeout, cancellationToken);

      ///<summary>The async condition wait <see cref="TryAwaitAsync"/> and <see cref="TryUpdateWhenAsync"/> share: <see cref="Monitor.Wait(object)"/>'s three steps<br/>
      /// taken apart, with its middle one, waiting for the notification, done by awaiting <see cref="_nextUpdate"/> instead of parking a thread.</summary>
      ///<remarks>Only an evaluation that finds <paramref name="condition"/> true releases the lock as <paramref name="heldLock"/>, which for an update lock wakes the<br/>
      /// other waits. One that finds it false changed nothing and releases it as a read lock: were it to wake the others, two waits with false<br/>
      /// conditions would keep waking each other.</remarks>
      async Task<bool> TryExecuteWhenAsync<THeldLock>(Func<bool> condition, Action executeOnceTheConditionHolds, THeldLock heldLock, WaitTimeout? waitTimeout, LockTimeout? lockTimeout, CancellationToken cancellationToken)
         where THeldLock : class, IDisposable
      {
         State.Assert(!_monitor.IsHeldByTheCurrentThread, () => "A thread holding this monitor can't start an async condition wait on it: the wait could not release the lock while it waits, as a synchronous wait does, because it may resume on another thread.");

         var effectiveWaitTimeout = waitTimeout ?? WaitTimeout;
         var waitStartedAt = DateTime.UtcNow;

         while(true)
         {
            Task nextUpdate;
            var conditionHolds = false;
            TakeLock(heldLock, lockTimeout, cancellationToken);
            try
            {
               conditionHolds = condition();
               if(conditionHolds)
               {
                  executeOnceTheConditionHolds();
                  return true;
               }

               cancellationToken.ThrowIfCancellationRequested();
               if(HasExpired()) return false;
               nextUpdate = NextUpdate();
            }
            finally
            {
               if(conditionHolds) heldLock.Dispose();
               else ReleaseLock();
            }

            //Suppressed so that timing out or being cancelled while awaiting the next update both lead back to evaluating the condition,
            //which the synchronous wait also evaluates once more before it gives up or throws.
            await nextUpdate.WaitAsync(TimeRemaining(), cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
         }

         bool HasExpired() => !effectiveWaitTimeout.IsInfinite && effectiveWaitTimeout.IsExpired(waitStartedAt);
         TimeSpan TimeRemaining() => effectiveWaitTimeout.IsInfinite ? Timeout.InfiniteTimeSpan : effectiveWaitTimeout.TimeRemaining(waitStartedAt);
      }

      ///<summary>The task the next update-lock release completes. Called while holding the lock, so no update can come between an evaluation of<br/>
      /// a condition that found it false and the wait for the next update.</summary>
      Task NextUpdate() => (_nextUpdate ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).Task;

      ///<summary>Wakes the async condition waits, so they evaluate their conditions again. Called by <see cref="ReleaseUpdateLock"/> while it still holds the lock.<br/>
      /// The woken waits continue on the thread pool, never inline on the releasing thread, which holds the lock: <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/>.</summary>
      void NotifyAsyncWaitsAboutUpdates()
      {
         var nextUpdate = _nextUpdate;
         if(nextUpdate == null) return;
         _nextUpdate = null;
         nextUpdate.SetResult();
      }
   }
}
