using System.Collections.Generic;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

interface IExecutingTessagesSnapshot
{
    IReadOnlyList<TransportTessage.InComing> AtMostOnceTommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents { get; }
    IReadOnlyList<TransportTessage.InComing> ExecutingNonTransactionalQueries { get; }
}