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
         //A holder that never releases the lock, such as a thread stuck in a native call, is never named: the stack comes only from
         //   MonitorCE's ReleaseLock. Recording the holding thread and when it took the lock, at each take, was measured 2026-11-24
         //   with Compze.Tests.Benchmarks' MonitorBenchmarks: a monitor grew from 152 to 168 bytes, and taking and releasing a read
         //   lock went from 13.6 to 17.7 ns, an update lock from 21.5 to 25.7 ns. Magnus's bar was no added memory and no notable
         //   slowdown, so it isn't done.
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
