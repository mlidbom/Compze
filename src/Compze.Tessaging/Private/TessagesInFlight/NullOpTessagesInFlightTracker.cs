using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Internal.TessagesInFlight;
using Compze.Tessaging.Private.Transport;
using Compze.Threading;

namespace Compze.Tessaging.Private.TessagesInFlight;

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
