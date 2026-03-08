using Compze.Core.Tessaging.Transport.Internal;

namespace Compze.Typermedia.Client;

public interface ITypermediaRouter : ITypermediaRouting
{
    Task ConnectAsync(EndPointAddress endpointAddress);
    void Start();
    void Stop();
}
