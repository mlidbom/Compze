using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// Owns a set of <see cref="IEndpoint"/>s and drives their shared lifecycle. The host's ordering guarantees
/// are host-wide: when <see cref="StartAsync"/> runs, every endpoint's components start listening before any
/// component anywhere in the host announces its address, and every address is announced before any component
/// starts sending — so no endpoint can send to a sibling that is not yet ready to receive, and the first look
/// any sender takes at discovery already sees every endpoint its host announced. See
/// <see cref="IEndpointComponent"/> for the per-component contract.
///
/// Production code creates a host via <c>EndpointHost.Production.Create</c> (in Compze.Hosting); tests use
/// <see cref="ITestingEndpointHost"/>.
///</summary>
public interface IEndpointHost : IAsyncDisposable
{
    ///<summary>Declares an endpoint. The <paramref name="setup"/> callback receives the endpoint's <see cref="IEndpointBuilder"/>: add capabilities (such as <c>AddExactlyOnceTessaging()</c> / <c>AddDistributedTypermedia()</c>), register handlers, and register the endpoint's own components.</summary>
    IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup);

    ///<summary>Registers a composed endpoint with the host, which owns it from here: the host drives its lifecycle phases<br/>
    /// host-wide and disposes it. The callback receives a fresh container builder from the host's container factory and<br/>
    /// returns the endpoint composed on it — e.g. <c>host.RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(container, ...))</c>.<br/>
    /// The host never knows the endpoint's tier: what the endpoint is, is decided entirely by its composition.</summary>
    TEndpoint RegisterEndpoint<TEndpoint>(Func<IContainerBuilder, TEndpoint> composeEndpoint) where TEndpoint : IEndpoint;

    ///<summary>The endpoints registered with this host so far, in registration order.</summary>
    IReadOnlyList<IEndpoint> Endpoints { get; }

    ///<summary>Starts every registered endpoint: all listening components host-wide, then all address announcements, then all sending components.</summary>
    Task StartAsync();

    ///<summary>Synchronous wrapper for <see cref="StartAsync"/>.</summary>
    void Start();
}
