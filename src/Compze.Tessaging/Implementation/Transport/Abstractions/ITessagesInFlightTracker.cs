using Compze.Abstractions.Hosting.Public;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

public interface ITessagesInFlightTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);
    void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride);
    void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception);
    ///<summary>A queued tessage was dropped without being handled by <paramref name="remoteEndpointId"/> — on the best-effort tier,<br/>
    /// the one tessage in flight at a delivery failure (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>).<br/>
    /// Nothing further will happen with it, so it is no longer in flight to that endpoint.</summary>
    void DroppedBeforeDelivery(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);
}
