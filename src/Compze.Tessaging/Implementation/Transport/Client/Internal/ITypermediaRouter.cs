using Compze.Core.Tessaging.Transport.Internal;
using Compze.Typermedia;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

public interface ITypermediaRouter : ITypermediaRouting
{
    Task DiscoverAndConnectAsync(EndPointAddress seedAddress);
    void Start();
    void Stop();
}
