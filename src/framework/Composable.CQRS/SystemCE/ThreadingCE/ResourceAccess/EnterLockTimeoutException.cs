using System;
using System.Diagnostics;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess;

public class EnterLockTimeoutException : Exception
{
   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
   readonly TimeSpan _timeToWaitForOwningThreadStacktrace;
   string? _blockingThreadStacktrace;

   internal EnterLockTimeoutException(TimeSpan timeout, TimeSpan? stackTraceFetchTimeout) : base($"Timed out awaiting lock after {timeout}. This likely indicates an in-memory deadlock. See below for the stacktrace of the blocking thread as it disposes the lock.") =>
      _timeToWaitForOwningThreadStacktrace = stackTraceFetchTimeout ?? 1.Seconds();

   public override string Message
   {
      get
      {
         //BUG: Blocking loggers and similar in production is not OK: We need to find a different way of getting this into the logs that do not do that
         if(!_monitor.TryAwait(_timeToWaitForOwningThreadStacktrace, () => _blockingThreadStacktrace != null))
         {
            _blockingThreadStacktrace = $"Failed to get blocking thread stack trace. Timed out after: {_timeToWaitForOwningThreadStacktrace}";
         }
         return $@"{base.Message}
----- Blocking thread lock disposal stack trace-----
{_blockingThreadStacktrace}
";
      }
   }

   internal void SetBlockingThreadsDisposeStackTrace(StackTrace blockingThreadStackTrace) =>
      _monitor.Update(() => _blockingThreadStacktrace = blockingThreadStackTrace.ToString());
}