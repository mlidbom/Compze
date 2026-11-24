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
         //review: a holder that never releases the lock, such as a thread stuck in a native call, is never named: the stack comes only
         //   from MonitorCE's ReleaseLock, so the message says only that it failed to get one. It happened 2026-11-22 with the Claude
         //   tooling hub's hang in AttachConsole, and diagnosing it took a dump. Record the holding thread's managed id, its name and
         //   when it took the lock at each take, a few field writes, and put them in every timeout's message? A stack captured at the
         //   take could be opt-in. Recommended by the Claude tooling's sessions, passed on by Magnus (Claude, 2026-11-24).
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
