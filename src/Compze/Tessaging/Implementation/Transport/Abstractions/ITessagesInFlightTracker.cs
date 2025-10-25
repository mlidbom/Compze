using System;
using System.Collections.Generic;
using Compze.Core.Tessaging.Hosting.Public;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

interface ITessagesInFlightTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingTessageOnTransport(TransportTessage.OutGoing transportTessage, EndpointId remoteEndpointId);
    void AwaitNoTessagesInFlight(TimeSpan? timeoutOverride);
    void DoneWith(TransportTessage.InComing tessage, EndpointId handlingEndpointId, Exception? exception);
}