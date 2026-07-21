using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.TessageBus.Private.Inbox;

interface IExecutingTessagesSnapshot
{
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents { get; }
}
