using System;
using System.Diagnostics;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess;

public class EnterLockTimeoutException : Exception
{
   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
   readonly TimeSpan _timeToWaitForOwningThreadStacktrace;
   internal ulong LockId { get; }
   string? _blockingThreadStacktrace;

   internal EnterLockTimeoutException(ulong lockId, TimeSpan timeout, TimeSpan? stackTraceFetchTimeout) : base($"Timed out awaiting lock after {timeout}. This likely indicates an in-memory deadlock. See below for the stacktrace of the blocking thread as it disposes the lock.")
   {
      _timeToWaitForOwningThreadStacktrace = stackTraceFetchTimeout ?? 1.Seconds();
      LockId = lockId;
   }

   public override string Message
   {
      get
      {
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