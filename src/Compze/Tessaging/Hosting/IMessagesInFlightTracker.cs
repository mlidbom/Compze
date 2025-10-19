using System;
using System.Collections.Generic;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.Hosting;

interface IMessagesInFlightTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage, EndpointId remoteEndpointId);
    void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    void DoneWith(Guid messageId, EndpointId handlingEndpointId, Exception? exception);
}