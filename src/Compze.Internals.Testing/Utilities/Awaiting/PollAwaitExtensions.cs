using System.Runtime.CompilerServices;
using Compze.Threading;

namespace Compze.Internals.Testing.Utilities.Awaiting;

///<summary>Waits for a condition about any object to become true by asking it repeatedly — the honest wait for state that
/// publishes no signal to block on.</summary>
///<remarks>A specification must never continue on a timer without having observed the thing it was waiting for. Sleeping a
/// guessed duration and then asserting breaks that rule twice over: it wastes the time the guess was too long, and when the guess
/// is too short it does not fail — it asserts against state that never arrived, so the specification passes having proven nothing.
/// Polling cannot do either. It returns as soon as the condition holds, and it cannot reach the code after the wait without having
/// seen the condition true, because failing to see it throws.</remarks>
///<remarks>Prefer blocking on a signal where one exists — <see cref="Compze.Threading.ResourceAccess.IAwaitableShared{TShared}"/>
/// and the thread gates wake the instant the state changes and cost nothing while waiting. Polling is for the state that offers
/// no such signal, and it earns its keep by never requiring the observed object to cooperate: no interface to implement, no event
/// to raise, and — the point — nothing added to production code for a test's benefit.</remarks>
public static class PollAwaitExtensions
{
   ///<summary>How long a poll-await waits before giving up, unless the caller says otherwise: 30 seconds. Long enough that no
   /// correct wait ever reaches it, short enough that a broken one reports rather than hangs the suite.</summary>
   ///<remarks>Deliberately not <see cref="WaitTimeout.Default"/>, which is <see cref="WaitTimeout.Infinite"/>: a specification
   /// that waits forever for something that will never happen tells us nothing and blocks everything behind it.</remarks>
   public static readonly WaitTimeout DefaultPollAwaitTimeout = WaitTimeout.Seconds(30);

   extension<TThis>(TThis @this)
   {
      ///<summary>Blocks until <paramref name="condition"/> is true of this object, evaluating it every
      /// <paramref name="pollInterval"/>. Throws <see cref="PollAwaitTimeoutException"/> if it has not become true within
      /// <paramref name="timeout"/>.</summary>
      public void PollAwait(Func<TThis, bool> condition,
                            PollingInterval? pollInterval = null,
                            WaitTimeout? timeout = null,
                            [CallerArgumentExpression(nameof(condition))] string? conditionExpression = null)
      {
         if(!@this.TryPollAwait(condition, pollInterval, timeout))
            throw new PollAwaitTimeoutException(@this, conditionExpression, timeout ?? DefaultPollAwaitTimeout);
      }

      ///<summary>Blocks until <paramref name="condition"/> is true of this object, evaluating it every
      /// <paramref name="pollInterval"/>. Returns true as soon as it holds, or false if <paramref name="timeout"/> expires
      /// first.</summary>
      ///<remarks>For the wait whose expiry is the specified outcome — "no second tessage arrives" — where a timeout is the
      /// expected result rather than a failure. A wait that expects to succeed uses <see cref="PollAwait"/>, whose throw carries
      /// the diagnosis.</remarks>
      public bool TryPollAwait(Func<TThis, bool> condition, PollingInterval? pollInterval = null, WaitTimeout? timeout = null)
      {
         var interval = pollInterval ?? PollingInterval.Default;
         var waitTimeout = timeout ?? DefaultPollAwaitTimeout;
         var waitStarted = DateTime.UtcNow;

         while(true)
         {
            if(condition(@this)) return true;                                        // always evaluated before giving up, so false means observed-false
            if(!waitTimeout.IsInfinite && DateTime.UtcNow - waitStarted >= waitTimeout.Value) return false;
            Thread.Sleep(interval);
         }
      }
   }
}
