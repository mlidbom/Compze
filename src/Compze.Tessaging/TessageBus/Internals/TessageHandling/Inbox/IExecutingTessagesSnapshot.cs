using Compze.Tessaging.Internals.Transport.Abstractions;

namespace Compze.Tessaging.TessageBus.Internals.TessageHandling.Inbox;

public interface IExecutingTessagesSnapshot
{
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents { get; }
}
