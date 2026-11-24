using Compze.Contracts;

using Compze.Threading.Interprocess._internal;

namespace Compze.Threading.Interprocess;

/// <summary>
/// Decides how long a thread awaiting an <see cref="InterprocessSignal"/> sleeps between polls of the signal's counter.<br/>
/// This is the latency-versus-power trade-off knob for interprocess waits: short intervals detect a signal quickly,
/// but each poll wakes the CPU, and frequent wakeups prevent it from reaching its deep low-power idle states —
/// a real battery-life cost on laptops even though the CPU usage is negligible.
/// </summary>
/// <remarks>
/// <see cref="Default"/> resolves the trade-off with a backoff curve: it polls eagerly at the start of a wait, when the signal
/// is most likely to arrive quickly, and backs off as the wait grows, capping the interval — and thereby the worst-case added
/// signal-detection latency — at 50 milliseconds. Use <see cref="WithMaxSignalLatency"/> to choose a different cap,
/// or implement the interface to plug in any schedule you can imagine.
/// </remarks>
public interface ISignalPollingPolicy
{
   ///<summary>Given how long the caller has been awaiting the signal so far, returns how long to sleep before polling again.<br/>
   /// Called concurrently by every awaiting thread, so implementations must be thread-safe; a pure function of <paramref name="elapsedWaitTime"/> is the natural shape.</summary>
   TimeSpan NextPollInterval(TimeSpan elapsedWaitTime);

   ///<summary>The policy used when none is specified: <see cref="WithMaxSignalLatency"/> with a 50 millisecond cap.</summary>
   public static ISignalPollingPolicy Default { get; } = WithMaxSignalLatency(CappedQuarterOfElapsedWaitTimePollingPolicy.DefaultMaxSignalLatencyTarget);

   ///<summary>Returns a policy that sleeps a quarter of the time waited so far, never less than 1 millisecond and never more than
   /// <paramref name="maxSignalLatencyTarget"/> — which thereby becomes the worst-case added signal-detection latency during a long wait.</summary>
   public static ISignalPollingPolicy WithMaxSignalLatency(TimeSpan maxSignalLatencyTarget) => CappedQuarterOfElapsedWaitTimePollingPolicy.CappedAt(maxSignalLatencyTarget);

   private sealed class CappedQuarterOfElapsedWaitTimePollingPolicy : ISignalPollingPolicy
   {
      internal static readonly TimeSpan DefaultMaxSignalLatencyTarget = TimeSpan.FromMilliseconds(50);
      static readonly TimeSpan MinimumPollInterval = TimeSpan.FromMilliseconds(1);

      readonly TimeSpan _pollingIntervalCap;

      internal static CappedQuarterOfElapsedWaitTimePollingPolicy CappedAt(TimeSpan cap) => new(cap);

      CappedQuarterOfElapsedWaitTimePollingPolicy(TimeSpan pollingIntervalCap)
      {
         Argument.Assert(pollingIntervalCap >= MinimumPollInterval);
         _pollingIntervalCap = pollingIntervalCap;
      }

      public TimeSpan NextPollInterval(TimeSpan elapsedWaitTime)
      {
         var quarterOfElapsedWait = elapsedWaitTime / 4;
         return TimeSpan.FromTicks(Math.Clamp(quarterOfElapsedWait.Ticks, MinimumPollInterval.Ticks, _pollingIntervalCap.Ticks));
      }
   }
}
