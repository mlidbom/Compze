using System.Diagnostics;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Internals.SystemCE.ThreadingCE.Async;

public class AsyncLockTimeoutException : Exception
{
   readonly IAwaitableLock _lock = IAwaitableLock.WithDefaultTimeout();
   readonly WaitTimeout _timeToWaitForOwningThreadStacktrace;
   string? _blockingThreadStacktrace;

   internal AsyncLockTimeoutException(LockTimeout timeout, WaitTimeout stackTraceFetchTimeout) : base($"Timed out awaiting async lock after {timeout}. This likely indicates an in-memory deadlock. See below for the stacktrace of the blocking thread as it disposes the lock.") =>
      _timeToWaitForOwningThreadStacktrace = stackTraceFetchTimeout;

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
