using Compze.Tessaging.Endpoints;
using Compze.Tessaging._internal.TessagesInFlight;
using Compze.Tessaging._private.Transport;
using Compze.Threading;

namespace Compze.Tessaging._private.TessagesInFlight;

class NullOpTessagesInFlightTracker : ITessagesInFlightTracker
{
   public IReadOnlyList<Exception> GetExceptions() => [];
   public void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) {}
   public void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride) {}
   public void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception) {}
   public void DroppedBeforeDelivery(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId) {}
   public void TeventObservationQueued(Type wrapperTeventType) {}
   public void TeventObservationDispatched(Type wrapperTeventType) {}
}
