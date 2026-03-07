using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface IExecutingTessagesSnapshot
{
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTommands { get; }
    IReadOnlyList<TransportTessage.InComing> ExactlyOnceTevents { get; }
}