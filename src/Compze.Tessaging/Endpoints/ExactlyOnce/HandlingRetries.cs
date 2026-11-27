using Compze.Contracts;

namespace Compze.Tessaging.Endpoints.ExactlyOnce;

///<summary>How long an exactly-once endpoint keeps trying a tessage whose handling fails before it marks the tessage failed:<br/>
/// <see cref="For"/> a time, 5 minutes unless the endpoint declares otherwise, or <see cref="UntilHandled"/>. Declared on the<br/>
/// endpoint, through <see cref="ExactlyOnceEndpointBuilder.HandlingRetries"/> or<br/>
/// <see cref="ExactlyOnceEndpointDeclaration{TIdentity}.HandlingRetries"/>.</summary>
///<remarks>An exactly-once endpoint handles its exactly-once tessages one at a time, in the order they arrived, so every<br/>
/// tessage behind one that is being tried again waits for it. Marking a tessage failed lets the endpoint go on with them, and<br/>
/// gives up the failed tessage's place in the order: it isn't handled again, not after a restart either, until an<br/>
/// administrator retries it, once the cause of its failure is fixed. <see cref="UntilHandled"/> keeps the order whatever it<br/>
/// costs the tessages behind it.</remarks>
///<remarks>The time that counts is the tessage's failing time: how long its handling has been failing while the endpoint runs<br/>
/// and can reach its database. It carries across restarts. Neither the time the endpoint is down counts, nor the time its own<br/>
/// database is unreachable: then it is the endpoint that is failing, not the tessage. When the retries run out, the last try<br/>
/// is made as they do. The delays between the tries are the endpoint's to choose: they begin short and grow.</remarks>
public sealed class HandlingRetries
{
   //review-naming: HandlingRetries, For and UntilHandled are public names chosen on the session's own judgment (2026-11-27).
   readonly TimeSpan? _failingTimeAllowed;

   HandlingRetries(TimeSpan? failingTimeAllowed) => _failingTimeAllowed = failingTimeAllowed;

   ///<summary>Tries a tessage whose handling fails until its handling has failed for <paramref name="failingTime"/>, then marks<br/>
   /// it failed. <see cref="TimeSpan.Zero"/> marks it failed at its first failure.</summary>
   public static HandlingRetries For(TimeSpan failingTime)
   {
      Argument.Assert(failingTime >= TimeSpan.Zero);
      return new HandlingRetries(failingTime);
   }

   ///<summary>Tries a tessage whose handling fails until it is handled, however long that takes: the endpoint never marks a<br/>
   /// tessage failed, and nothing behind it is handled until it is.</summary>
   public static HandlingRetries UntilHandled { get; } = new(failingTimeAllowed: null);

   ///<summary>What an endpoint that declares nothing gets: 5 minutes of failing, which outlasts a database failover or a short<br/>
   /// network outage, and holds the tessages behind a tessage that keeps failing for no longer than that.</summary>
   internal static HandlingRetries Default { get; } = For(TimeSpan.FromMinutes(5));

   internal bool AreExhaustedAfterFailingFor(TimeSpan failingTime) => _failingTimeAllowed is {} allowed && failingTime >= allowed;

   ///<summary><paramref name="delay"/>, shortened so that the next try is made no later than when the retries run out.</summary>
   internal TimeSpan ShortenedToTheTimeLeft(TimeSpan delay, TimeSpan failingTime) =>
      _failingTimeAllowed is {} allowed && allowed - failingTime < delay ? allowed - failingTime : delay;

   public override string ToString() => _failingTimeAllowed is {} allowed ? $"for {allowed} of failing" : "until handled";
}
