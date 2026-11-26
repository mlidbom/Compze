using System.Diagnostics;

namespace Compze.Threading._internal;

///<summary>A wait that has begun: its <see cref="WaitTimeout"/>, and when it began. A wait that waits in several steps, such as one that polls or one<br/>
/// woken to evaluate its condition again, asks it how much of its time is left.</summary>
///<remarks>Timed by <see cref="Stopwatch"/>, not <see cref="DateTime.UtcNow"/>: setting the system clock neither shortens nor lengthens a wait.</remarks>
readonly struct StartedWait
{
   readonly WaitTimeout _waitTimeout;
   readonly long _beganAt;

   StartedWait(WaitTimeout waitTimeout)
   {
      _waitTimeout = waitTimeout;
      _beganAt = Stopwatch.GetTimestamp();
   }

   internal static StartedWait Begin(WaitTimeout waitTimeout) => new(waitTimeout);

   ///<summary>How long the wait has waited so far.</summary>
   internal TimeSpan Elapsed => Stopwatch.GetElapsedTime(_beganAt);

   ///<summary>True once all of the wait's <see cref="WaitTimeout"/> has passed. Never for a wait of <see cref="WaitTimeout.Infinite"/>.</summary>
   internal bool HasExpired => !_waitTimeout.IsInfinite && Elapsed >= _waitTimeout.Value;

   ///<summary>What is left of the wait's <see cref="WaitTimeout"/>, none once it has expired: <see cref="WaitTimeout.Infinite"/> for an infinite wait.</summary>
   internal WaitTimeout TimeRemaining => _waitTimeout.IsInfinite
                                            ? WaitTimeout.Infinite
                                            : new WaitTimeout(TimeSpan.FromTicks(Math.Max(0, (_waitTimeout.Value - Elapsed).Ticks)));

   ///<summary>What is left of the wait, but no more than <paramref name="longest"/>: how long the wait's next step may take, so that a wait that ends<br/>
   /// at its timeout neither overshoots it nor sleeps longer than a step should.</summary>
   internal TimeSpan TimeRemainingButNoMoreThan(TimeSpan longest)
   {
      if(_waitTimeout.IsInfinite) return longest;
      var timeRemaining = TimeRemaining.Value;
      return timeRemaining < longest ? timeRemaining : longest;
   }
}
