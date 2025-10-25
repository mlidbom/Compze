using System.Collections.Generic;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

interface IExecutingTessagesSnapshot
{
    IReadOnlyList<TransportTessage.InComing> AtMostOnceCommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceCommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceEvents { get; }
    IReadOnlyList<TransportTessage.InComing> ExecutingNonTransactionalQueries { get; }
}