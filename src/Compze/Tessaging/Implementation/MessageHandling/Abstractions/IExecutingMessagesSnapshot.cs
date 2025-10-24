using System.Collections.Generic;

namespace Compze.Tessaging.Implementation.MessageHandling.Abstractions;

interface IExecutingMessagesSnapshot
{
    IReadOnlyList<TransportMessage.InComing> AtMostOnceCommands { get; }
    IReadOnlyList<TransportMessage.InComing> ExactlyOnceCommands { get; }
    IReadOnlyList<TransportMessage.InComing> ExactlyOnceEvents { get; }
    IReadOnlyList<TransportMessage.InComing> ExecutingNonTransactionalQueries { get; }
}