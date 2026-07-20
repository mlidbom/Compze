using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine.Internal;
using Compze.Tessaging.Internal.Transport;
using Compze.Threading;

namespace Compze.Tessaging.Internal.TessagesInFlight;

///<summary>The testing device that answers whether any tessaging work is in flight in the compositions reporting to it:<br/>
/// transport tessages sent but not yet handled by every destination, and tevents queued for observation but not yet dispatched<br/>
/// (the engine's <see cref="TeventObservationDispatcher"/> reports those transitions). The testing host hands one tracker to<br/>
/// every endpoint it registers and awaits its at-rest on dispose, so a test cannot pass with work still in flight; production<br/>
/// compositions run the null tracker — the production-honest await is quiescence, not this device.</summary>
interface ITessagesInFlightTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);
    void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride);
    void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception);
    ///<summary>A queued tessage was dropped without being handled by <paramref name="remoteEndpointId"/> — on the best-effort tier,<br/>
    /// the one tessage in flight at a delivery failure (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>).<br/>
    /// Nothing further will happen with it, so it is no longer in flight to that endpoint.</summary>
    void DroppedBeforeDelivery(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);

    ///<summary>A tevent was queued on one observer's dispatch queue — observation work now in flight until the matching<br/>
    /// <see cref="TeventObservationDispatched"/>. Reported per matching observer: a tevent three observers observe is three<br/>
    /// queued observations.</summary>
    void TeventObservationQueued(Type wrapperTeventType);
    ///<summary>One queued observation of <paramref name="wrapperTeventType"/> was dispatched: its observer invocation has<br/>
    /// completed — however it fared, since a throwing observer's failure surfaces through the background-exception reporter,<br/>
    /// not through the tracker.</summary>
    void TeventObservationDispatched(Type wrapperTeventType);
}
