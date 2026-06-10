
namespace Compze.Abstractions.Hosting.Public;

public interface IEndpointHost : IAsyncDisposable
{
    IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);
    Task StartAsync();
    void Start();
}
