using System.Diagnostics;

namespace Compze.Threading.Interprocess._private.ProcessLocks;

///<summary>One take's wait for an <see cref="IProcessLock"/>: how long it may last, and the pause between its tries to take the lock.</summary>
sealed class ProcessLockWait
{
   ///<summary>How long a wait pauses between its tries to take the lock: the longest a waiter takes to notice a release. Fixed, rather than<br/>
   /// growing as the wait grows, which would hand the lock to whoever began waiting last.</summary>
   static readonly TimeSpan TimeBetweenTries = TimeSpan.FromMilliseconds(50);

   readonly WaitTimeout _waitTimeout;
   readonly CancellationToken _cancellationToken;
   readonly long _beganAt = Stopwatch.GetTimestamp();

   ProcessLockWait(WaitTimeout waitTimeout, CancellationToken cancellationToken)
   {
      _waitTimeout = waitTimeout;
      _cancellationToken = cancellationToken;
   }

   ///<summary>The wait, begun: throws an <see cref="OperationCanceledException"/> if <paramref name="cancellationToken"/> is already cancelled.</summary>
   internal static ProcessLockWait Begin(WaitTimeout waitTimeout, CancellationToken cancellationToken)
   {
      cancellationToken.ThrowIfCancellationRequested();
      return new ProcessLockWait(waitTimeout, cancellationToken);
   }

   ///<summary>Pauses until the next try, and returns true: or returns false at once if the wait has ended. Throws an<br/>
   /// <see cref="OperationCanceledException"/> as soon as the wait is cancelled, mid-pause too, as it waits on the cancellation's handle.</summary>
   internal bool PausedUntilTheNextTry()
   {
      if(PauseBeforeTheNextTry() is not {} pause) return false;

      _cancellationToken.WaitHandle.WaitOne(pause);
      _cancellationToken.ThrowIfCancellationRequested();
      return true;
   }

   ///<summary>As <see cref="PausedUntilTheNextTry"/>, holding no thread while it pauses. Cancellation surfaces as the<br/>
   /// <see cref="OperationCanceledException"/> the synchronous pause throws, not as <see cref="Task.Delay(TimeSpan, CancellationToken)"/>'s<br/>
   /// <see cref="TaskCanceledException"/>.</summary>
   internal async Task<bool> PausedUntilTheNextTryAsync()
   {
      if(PauseBeforeTheNextTry() is not {} pause) return false;

      await Task.Delay(pause, _cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
      _cancellationToken.ThrowIfCancellationRequested();
      return true;
   }

   ///<summary><see cref="TimeBetweenTries"/>, but never past the end of the wait, so that a wait that ends has tried once more at its end. Null once<br/>
   /// the wait has ended.</summary>
   TimeSpan? PauseBeforeTheNextTry()
   {
      //refactor: IAwaitableMutex.AwaitableMutex.NextAwaitChunkTimeout and HasExpired, and IAwaitableMonitor's async condition waits, compute the
      //   same: a WaitTimeout's time left since a wait began, cut to a poll interval. They pass the WaitTimeout and a DateTime start around
      //   instead, through WaitTimeout.IsExpired and WaitTimeout.TimeRemaining. One internal type in Compze.Threading for a wait that has begun,
      //   timed by Stopwatch as this is, would serve them all, and this would keep only its TimeBetweenTries.
      if(_waitTimeout.IsInfinite) return TimeBetweenTries;

      var timeLeft = _waitTimeout.Value - Stopwatch.GetElapsedTime(_beganAt);
      if(timeLeft <= TimeSpan.Zero) return null;
      return timeLeft < TimeBetweenTries ? timeLeft : TimeBetweenTries;
   }
}
