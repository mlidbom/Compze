using Compze.ServiceBus.Implementation.Transport.Abstractions;

namespace Compze.ServiceBus.Implementation.TessageHandling.Abstractions;

public interface IExecutingTessagesSnapshot
{
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents { get; }
}