using Compze.Contracts;

namespace Compze.Tessaging.Endpoints.ExactlyOnce;

///<summary>How an exactly-once endpoint retries a tessage whose handling fails: with a delay that grows, until it gives up and<br/>
/// marks the tessage failed, <see cref="GiveUpAfter"/> a time, 5 minutes unless the endpoint declares otherwise, or never, with<br/>
/// <see cref="RetryForever"/>. Declared on the endpoint, through <see cref="ExactlyOnceEndpointBuilder.RetryPolicy"/> or<br/>
/// <see cref="ExactlyOnceEndpointDeclaration{TIdentity}.RetryPolicy"/>.</summary>
///<remarks>An exactly-once endpoint handles its exactly-once tessages one at a time, in the order they arrived, so every<br/>
/// tessage behind one that is being tried again waits for it. Marking a tessage failed lets the endpoint go on with them, and<br/>
/// gives up the failed tessage's place in the order: it isn't handled again, not after a restart either, until an<br/>
/// administrator retries it, once the cause of its failure is fixed. <see cref="RetryForever"/> keeps the order whatever it<br/>
/// costs the tessages behind it.</remarks>
///<remarks>The time that counts is the time spent retrying: how long the tessage's handling has been failing while the endpoint<br/>
/// runs and can reach its database. It carries across restarts. Neither the time the endpoint is down counts, nor the time its<br/>
/// own database is unreachable: then it is the endpoint that is failing, not the tessage. As the policy gives up, the last try<br/>
/// is made. The delays between the tries are the endpoint's to choose: they begin short and grow.</remarks>
public sealed class RetryPolicy
{
   readonly TimeSpan? _failingTimeAllowed;

   RetryPolicy(TimeSpan? failingTimeAllowed) => _failingTimeAllowed = failingTimeAllowed;

   ///<summary>Retries a tessage whose handling fails until it has been retried for <paramref name="maxTimeToRetry"/>, then gives<br/>
   /// up and marks it failed. <see cref="TimeSpan.Zero"/> gives up at its first failure.</summary>
   public static RetryPolicy GiveUpAfter(TimeSpan maxTimeToRetry)
   {
      Argument.Assert(maxTimeToRetry >= TimeSpan.Zero);
      return new RetryPolicy(maxTimeToRetry);
   }

   ///<summary>Retries a tessage whose handling fails until it is handled, however long that takes: the endpoint never gives up<br/>
   /// on a tessage, and nothing behind it is handled until it is.</summary>
   public static RetryPolicy RetryForever { get; } = new(failingTimeAllowed: null);

   ///<summary>What an endpoint that declares nothing gets: giving up after 5 minutes of retrying, which outlasts a database<br/>
   /// failover or a short network outage, and holds the tessages behind a tessage that keeps failing for no longer than that.</summary>
   internal static RetryPolicy Default { get; } = GiveUpAfter(TimeSpan.FromMinutes(5));

   internal bool GivesUpAfterFailingFor(TimeSpan failingTime) => _failingTimeAllowed is {} allowed && failingTime >= allowed;

   ///<summary>How long a tessage whose handling has failed for <paramref name="failingTime"/> may go on failing before its<br/>
   /// retries run out; null for <see cref="RetryForever"/>.</summary>
   internal TimeSpan? TimeLeftAfterFailingFor(TimeSpan failingTime) => _failingTimeAllowed - failingTime;

   public override string ToString() => _failingTimeAllowed is {} allowed ? $"giving up after {allowed} of retrying" : "retrying forever";
}
