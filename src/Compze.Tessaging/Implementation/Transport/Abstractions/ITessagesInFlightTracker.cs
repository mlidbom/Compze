using Compze.Abstractions.Hosting.Public;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

public interface ITessagesInFlightTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);
    void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride);
    void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception);
    ///<summary>A queued tessage was dropped without being delivered to <paramref name="remoteEndpointId"/> — the transient stream's<br/>
    /// drop-stream-whole policy (see <c>src/Compze.Tessaging/_docs/tevent-delivery-model.md</c>). Nothing further will happen with it,<br/>
    /// so it is no longer in flight to that endpoint.</summary>
    void DroppedBeforeDelivery(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);
}
