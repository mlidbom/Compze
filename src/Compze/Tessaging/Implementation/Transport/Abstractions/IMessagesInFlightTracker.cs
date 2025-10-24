using System;
using System.Collections.Generic;
using Compze.Tessaging.Hosting.Abstractions.Transport;

namespace Compze.Tessaging.Implementation.Transport.Abstractions;

interface IMessagesInFlightTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage, EndpointId remoteEndpointId);
    void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    void DoneWith(TransportMessage.InComing message, EndpointId handlingEndpointId, Exception? exception);
}