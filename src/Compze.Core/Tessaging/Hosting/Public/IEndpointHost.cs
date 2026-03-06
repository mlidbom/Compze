using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface IEndpointHost : IAsyncDisposable
{
    IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);
    Task StartAsync();
    void Start();
}
