using System;
using System.Collections.Generic;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Tessaging.Hosting;

interface IGlobalBusStateTracker
{
    IReadOnlyList<Exception> GetExceptions();

    void SendingMessageOnTransport(TransportMessage.OutGoing transportMessage);
    void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride);
    void DoneWith(Guid message, Exception? exception);
}