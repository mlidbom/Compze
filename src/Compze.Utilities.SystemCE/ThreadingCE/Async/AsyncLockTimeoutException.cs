using System;
using System.Diagnostics;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Utilities.SystemCE.ThreadingCE.Async;

public class AsyncLockTimeoutException : Exception
{
   readonly IAwaitableMonitor _monitor = IAwaitableMonitor.WithDefaultTimeout();
   readonly WaitTimeout _timeToWaitForOwningThreadStacktrace;
   string? _blockingThreadStacktrace;

   public AsyncLockTimeoutException(LockTimeout timeout, WaitTimeout stackTraceFetchTimeout) : base($"Timed out awaiting async lock after {timeout}. This likely indicates an in-memory deadlock. See below for the stacktrace of the blocking thread as it disposes the lock.") =>
      _timeToWaitForOwningThreadStacktrace = stackTraceFetchTimeout;

   public override string Message
   {
      get
      {
         //Todo: Blocking loggers and similar in production is not great: This only happens on in-memory deadlocks though, so it does not seem too urgent.
         if(!_monitor.TryAwait(() => _blockingThreadStacktrace != null, _timeToWaitForOwningThreadStacktrace))
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

   public void SetBlockingThreadsDisposeStackTrace(StackTrace blockingThreadStackTrace) =>
      _monitor.Update(() => _blockingThreadStacktrace = blockingThreadStackTrace.ToString());
}
