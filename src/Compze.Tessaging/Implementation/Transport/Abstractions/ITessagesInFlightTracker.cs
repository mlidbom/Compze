using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

public interface ITessagesInFlightTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);
    void AwaitNoTessagesInFlight(WaitTimeout? timeoutOverride);
    void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception);
}