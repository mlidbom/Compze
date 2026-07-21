using Compze.Tessaging.TessageBus.Private.BestEffortDelivery;
using Compze.Tessaging.Endpoints;

namespace Compze.Tessaging.TessageBus.Exceptions;

///<summary>Thrown by a best-effort tevent publish when the tevent would exceed its peer's queue bound<br/>
/// (<see cref="BestEffortTeventQueues.MaximumQueuedTeventsPerPeer"/>). The queue grows while a remembered subscriber is down,<br/>
/// so hitting the bound means the peer has been down — or unable to keep up — for a long time. This is deliberate backpressure:<br/>
/// failing the publish loud, inside the caller's transaction, loses nothing, while silently shedding queued tevents does<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>). A peer that is gone for good should be decommissioned, which ends the<br/>
/// queueing on its behalf.</summary>
public class BestEffortTeventQueueOverflowException : Exception
{
   internal BestEffortTeventQueueOverflowException(EndpointId peerId) : base(
      $"Cannot queue another best-effort tevent for peer {peerId}: its queue is at the bound of {BestEffortTeventQueues.MaximumQueuedTeventsPerPeer} tevents (queued, plus reserved by uncommitted transactions). " +
      "The peer has been down, or unable to keep up, for long enough to exhaust the bound. The publish fails rather than tevents being silently shed; if the peer is gone for good, decommission it.") {}
}
