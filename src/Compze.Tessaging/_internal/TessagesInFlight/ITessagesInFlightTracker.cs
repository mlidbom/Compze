using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine._private;
using Compze.Threading;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging._internal.TessagesInFlight;

///<summary>The testing device that answers whether any tessaging work is in flight in the compositions reporting to it:<br/>
/// transport tessages sent but not yet handled by every destination, and tevents queued for observation but not yet dispatched<br/>
/// (the engine's <see cref="TeventObservationDispatcher"/> reports those transitions). The testing host hands one tracker to<br/>
/// every endpoint it registers and awaits its at-rest on dispose, so a test cannot pass with work still in flight; production<br/>
/// compositions run the null tracker — the production-honest await is quiescence, not this device.</summary>
interface ITessagesInFlightTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);
    ///<summary>Completes when nothing is in flight, holding no thread while it waits; throws when <paramref name="timeoutOverride"/>, or 10 seconds, passes first.</summary>
    Task AwaitNoTessagesInFlightAsync(WaitTimeout? timeoutOverride);
    void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception);
    ///<summary>The endpoint <paramref name="endpointId"/> admitted <paramref name="tessage"/>, which it sent itself, and its sender<br/>
    /// committed: in flight until the endpoint has handled it.</summary>
    void AdmittedTessageSentToItself(TransportTessage.InComing tessage, EndpointId endpointId);
    ///<summary>A try of <paramref name="tessage"/>'s handling in <paramref name="handlingEndpointId"/> failed with<br/>
    /// <paramref name="exception"/>, and the tessage is tried again or marked failed: still in flight. The at-rest wait's<br/>
    /// timeout shows each tessage's last failed try, so a test whose handler keeps failing says why it didn't come to rest.</summary>
    void FailedTry(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception exception);
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
