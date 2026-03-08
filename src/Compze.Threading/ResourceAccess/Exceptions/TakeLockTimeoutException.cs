using System.Diagnostics;

namespace Compze.Threading.ResourceAccess.Exceptions;

public class TakeLockTimeoutException(LockTimeout timeout, WaitTimeout stackTraceFetchTimeout) :
   Exception($"Timed out awaiting lock after {timeout}. This likely indicates an in-memory deadlock. See below for the stacktrace of the blocking thread as it disposes the lock.")
{
   readonly IAwaitableLock _lock = IAwaitableLock.WithDefaultTimeout();
   readonly WaitTimeout _timeToWaitForOwningThreadStacktrace = stackTraceFetchTimeout;
   string? _blockingThreadStacktrace;

   public override string Message
   {
      get
      {
         //Todo: Blocking loggers and similar in production is not great: This only happens on in-memory deadlocks though, so it does not seem too urgent.
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
