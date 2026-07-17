using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Typermedia.Client;

namespace Compze.Tessaging.Implementation.HandlerAvailability;

///<summary>The endpoint's waiting sends: a send whose type has no live, unambiguous route right now does not explode — it<br/>
/// waits, bounded by the endpoint's <see cref="HandlerAvailabilityPatience"/>, for the world to become right (a first contact,<br/>
/// a known peer's return, an ambiguity resolving), then proceeds normally; only exhausted patience fails loud, and the failure<br/>
/// says what was waited for, for how long, and what the peer memory remembers<br/>
/// (see <c>dev_docs/TODO/WIP/Tessaging/readiness-and-waiting-sends.md</c>).</summary>
///<remarks>Why it exists: every remote-facing request/response send used to race discovery at startup — sent before the<br/>
/// serving peer's advertisement was discovered, it exploded instantly, and applications were forced to hand-roll readiness<br/>
/// probes and retry loops around the framework. Waiting absorbs the startup race and steady-state churn (a handler endpoint<br/>
/// restarting mid-day creates a seconds-wide window with no route); it never absorbs a real misdeployment, which surfaces as<br/>
/// the loud, diagnostic patience-exhausted failure.</remarks>
///<remarks>Availability is re-checked against the router's routes and the peer memory on a short interval rather than pushed<br/>
/// by change signals: the wait window is rare and bounded, so the simplicity of polling wins over signal plumbing across the<br/>
/// router and the registry; the cost is at most one interval of added latency on a path that just waited out process startup.</remarks>
interface IHandlerAvailability
{
   ///<summary>The address of the one connected endpoint whose advertisement handles the typermedia type<br/>
   /// <paramref name="tessageType"/> — waiting, within patience, for exactly one to be connected. No route after patience<br/>
   /// throws <see cref="NoHandlerForTypermediaTypeException"/> telling known-but-down from never-seen by the peer memory;<br/>
   /// several live routes still ambiguous after patience throw <see cref="MultipleHandlersForTypermediaTypeException"/><br/>
   /// naming the endpoints — never a silent pick.</summary>
   Task<EndpointAddress> AwaitAddressOfTypermediaHandlerForAsync(Type tessageType);
}
