
namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// Owns a set of <see cref="IEndpoint"/>s and drives their shared lifecycle. The host's one ordering guarantee
/// is host-wide: when <see cref="StartAsync"/> runs, every endpoint's components start listening before any
/// component anywhere in the host starts sending — so no endpoint can send to a sibling that is not yet ready
/// to receive. See <see cref="IEndpointComponent"/> for the per-component contract.
///
/// Production code creates a host via <c>EndpointHost.Production.Create</c> (in Compze.Hosting); tests use
/// <see cref="ITestingEndpointHost"/>.
///</summary>
public interface IEndpointHost : IAsyncDisposable
{
    ///<summary>Declares an endpoint. The <paramref name="setup"/> callback receives the endpoint's <see cref="IEndpointBuilder"/>: add paradigm pipelines (such as <c>AddTessaging()</c> / <c>AddTypermedia()</c>), register handlers, and register the endpoint's own components.</summary>
    IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);

    ///<summary>The endpoints registered with this host so far, in registration order.</summary>
    IReadOnlyList<IEndpoint> Endpoints { get; }

    ///<summary>Starts every registered endpoint: all listening components host-wide, then all sending components.</summary>
    Task StartAsync();

    ///<summary>Synchronous wrapper for <see cref="StartAsync"/>.</summary>
    void Start();
}
