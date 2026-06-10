
namespace Compze.Abstractions.Hosting.Public;

public interface IEndpointHost : IAsyncDisposable
{
    IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);

    ///<summary>The endpoints registered with this host so far, in registration order.</summary>
    IReadOnlyList<IEndpoint> Endpoints { get; }

    Task StartAsync();
    void Start();
}
