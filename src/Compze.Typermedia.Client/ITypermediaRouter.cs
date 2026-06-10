using Compze.Abstractions.Hosting.Public;

namespace Compze.Typermedia.Client;

public interface ITypermediaRouter : ITypermediaRouting
{
    Task ConnectAsync(EndpointAddress endpointAddress);
    void Start();
    void Stop();
}
