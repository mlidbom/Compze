using System.Diagnostics;

namespace Compze.Threading.Exceptions;

///<summary>Thrown when a lock acquisition attempt times out. Includes the blocking thread's stack trace when available, to help diagnose deadlocks.</summary>
public class TakeLockTimeoutException : Exception
{
   readonly IAwaitableMonitor _monitor = IAwaitableMonitor.WithDefaultTimeout();
   readonly WaitTimeout _timeToWaitForOwningThreadStacktrace;
   string? _blockingThreadStacktrace;

   internal TakeLockTimeoutException(string message, WaitTimeout stackTraceFetchTimeout) : base(message) =>
      _timeToWaitForOwningThreadStacktrace = stackTraceFetchTimeout;

   ///<summary>The exception message, augmented with the blocking thread's lock-disposal stack trace (awaited briefly, then replaced with a timeout note if it cannot be obtained).</summary>
   public override string Message
   {
      get
      {
         //Todo:review: Blocking loggers and similar in production is not great: This only happens on deadlocks though, so it does not seem too urgent.
         if(!_monitor.TryAwait(() => _blockingThreadStacktrace != null, waitTimeout: _timeToWaitForOwningThreadStacktrace))
         {
            _blockingThreadStacktrace = $"Failed to get blocking thread stack trace. Timed out after: {_timeToWaitForOwningThreadStacktrace}";
         }

         return $"""
                 {base.Message}
                 ----- Blocking thread lock disposal stack trace-----
                 {_blockingThreadStacktrace}

                 """;
      }
   }

   internal void SetBlockingThreadsDisposeStackTrace(StackTrace blockingThreadStackTrace) =>
      _monitor.Update(() => _blockingThreadStacktrace = blockingThreadStackTrace.ToString());
}

///<summary>Thrown when an in-process monitor lock acquisition times out after <see cref="LockTimeout"/>.</summary>
class TakeMonitorLockTimeoutException(LockTimeout timeout, WaitTimeout stackTraceFetchTimeout)
   : TakeLockTimeoutException($"Timed out awaiting monitor lock after {timeout}. This likely indicates an in-memory deadlock.", stackTraceFetchTimeout);

