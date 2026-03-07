using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Typermedia.Client;

public interface ITypermediaRouter : ITypermediaRouting
{
    Task DiscoverAndConnectAsync(EndPointAddress seedAddress);
    void Start();
    void Stop();
}
