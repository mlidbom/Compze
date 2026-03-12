using System.Diagnostics;

namespace Compze.Threading.Exceptions;

///<summary>Thrown when a lock acquisition attempt times out. Includes the blocking thread's stack trace when available, to help diagnose deadlocks.</summary>
public class TakeLockTimeoutException : Exception
{
   readonly IAwaitableMonitor _lock = IAwaitableMonitor.WithDefaultTimeout();
   readonly WaitTimeout _timeToWaitForOwningThreadStacktrace;
   string? _blockingThreadStacktrace;

   internal TakeLockTimeoutException(string message, WaitTimeout stackTraceFetchTimeout) : base(message) =>
      _timeToWaitForOwningThreadStacktrace = stackTraceFetchTimeout;

   public override string Message
   {
      get
      {
         //Todo: Blocking loggers and similar in production is not great: This only happens on deadlocks though, so it does not seem too urgent.
         if(!_lock.TryAwait(() => _blockingThreadStacktrace != null, _timeToWaitForOwningThreadStacktrace))
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
      _lock.Update(() => _blockingThreadStacktrace = blockingThreadStackTrace.ToString());
}

///<summary>Thrown when an in-process monitor lock acquisition times out after <see cref="LockTimeout"/>.</summary>
class TakeMonitorLockTimeoutException(LockTimeout timeout, WaitTimeout stackTraceFetchTimeout)
   : TakeLockTimeoutException($"Timed out awaiting monitor lock after {timeout}. This likely indicates an in-memory deadlock.", stackTraceFetchTimeout);

///<summary>Thrown when a cross-process mutex lock acquisition times out after <see cref="LockTimeout"/>.</summary>
class TakeMutexLockTimeoutException(LockTimeout timeout, WaitTimeout stackTraceFetchTimeout)
   : TakeLockTimeoutException($"Timed out awaiting interprocess mutex lock after {timeout}. This likely indicates a cross-process deadlock.", stackTraceFetchTimeout);
