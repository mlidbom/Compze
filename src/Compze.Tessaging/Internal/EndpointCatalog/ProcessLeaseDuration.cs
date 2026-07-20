namespace Compze.Tessaging.Internal.EndpointCatalog;

///<summary>How long the endpoint's process lease stays valid without a heartbeat — the one knob the one-process-per-endpoint<br/>
/// rule turns on (<see cref="EndpointProcessLease"/>). The holder refreshes well inside the duration; a lease unrefreshed for<br/>
/// a whole duration belongs to a dead process and may be taken over. Declared per endpoint via<br/>
/// <c>ExactlyOnceEndpointBuilder.ProcessLeaseDuration</c>; defaults to 15 seconds — generous enough that an ordinary pause<br/>
/// (GC, load) never fakes a death, small enough that a crashed endpoint's restart waits seconds, not minutes.</summary>
class ProcessLeaseDuration
{
   internal static readonly ProcessLeaseDuration Default = new(TimeSpan.FromSeconds(15));

   public TimeSpan Duration { get; }

   ///<summary>Five heartbeats fit one lease term, so a single delayed beat never lets the lease go stale.</summary>
   public TimeSpan HeartbeatInterval => Duration / 5;

   ///<summary>What a claimant waits before declaring the holder alive and failing loud: one full lease duration — a dead<br/>
   /// holder's lease must go stale within it — plus two heartbeats' margin.</summary>
   public TimeSpan ClaimPatience => Duration + 2 * HeartbeatInterval;

   public ProcessLeaseDuration(TimeSpan duration) => Duration = duration;
}
