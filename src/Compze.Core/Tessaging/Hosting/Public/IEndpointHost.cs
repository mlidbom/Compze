using System;
using System.Threading.Tasks;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface IEndpointHost : IAsyncDisposable
{
    IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);
    IClient RegisterClient(Action<IEndpointBuilder>? setup = null);
    Task StartAsync();
    void Start();
}
