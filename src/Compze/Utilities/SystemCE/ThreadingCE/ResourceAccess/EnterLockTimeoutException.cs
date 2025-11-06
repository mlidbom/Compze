using System;
using System.Diagnostics;

namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

class EnterLockTimeoutException : Exception
{
   readonly IMonitorCE _monitor = IMonitorCE.WithDefaultTimeout();
   readonly TimeSpan _timeToWaitForOwningThreadStacktrace;
   string? _blockingThreadStacktrace;

   internal EnterLockTimeoutException(TimeSpan timeout, TimeSpan stackTraceFetchTimeout) : base($"Timed out awaiting lock after {timeout}. This likely indicates an in-memory deadlock. See below for the stacktrace of the blocking thread as it disposes the lock.") =>
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

   internal void SetBlockingThreadsDisposeStackTrace(StackTrace blockingThreadStackTrace) =>
      _monitor.Update(() => _blockingThreadStacktrace = blockingThreadStackTrace.ToString());
}
