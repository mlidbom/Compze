using System;
using System.Threading.Tasks;

namespace Compze.Core.Tessaging.Hosting.Public;

public interface IEndpointHost : IAsyncDisposable
{
    IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);
    ///<summary>Registers a default client endpoint with a host. Can be called only once per host instance.</summary>
    IEndpoint RegisterClientEndpoint(Action<IEndpointBuilder> setup);
    IClient RegisterClient(Action<IEndpointBuilder>? setup = null);
    Task StartAsync();
    void Start();
}
