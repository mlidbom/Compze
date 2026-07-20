using Compze.Tessaging.Internal.Transport.Abstractions;

namespace Compze.Tessaging.TessageBus.Internal.Inbox;

public interface IExecutingTessagesSnapshot
{
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents { get; }
}
