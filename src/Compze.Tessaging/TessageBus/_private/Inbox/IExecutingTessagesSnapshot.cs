using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

interface IExecutingTessagesSnapshot
{
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents { get; }
}
