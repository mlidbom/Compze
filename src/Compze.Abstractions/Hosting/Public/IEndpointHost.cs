using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// Owns a set of <see cref="IEndpoint"/>s and drives their shared lifecycle. The host's ordering guarantees
/// are host-wide: when <see cref="StartAsync"/> runs, every endpoint starts listening before any endpoint
/// anywhere in the host announces its address, and every address is announced before any endpoint
/// starts sending — so no endpoint can send to a sibling that is not yet ready to receive, and the first look
/// any sender takes at discovery already sees every endpoint its host announced. Stopping runs in reverse:
/// addresses are retracted before any sending stops, and sending stops before listening, so an address stops
/// being advertised before anything goes deaf.
///
/// Production code creates a host via <c>EndpointHost.Production.Create</c> (in Compze.Hosting); tests use
/// the testing host in Compze.Tessaging.Hosting.Testing.
///</summary>
public interface IEndpointHost : IAsyncDisposable
{
    ///<summary>Registers a composed endpoint with the host, which owns it from here: the host drives its lifecycle phases<br/>
    /// host-wide and disposes it. The callback receives a fresh container builder from the host's container factory and<br/>
    /// returns the endpoint composed on it — e.g. <c>host.RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(container, ...))</c>.<br/>
    /// The host never knows the endpoint's tier: what the endpoint is, is decided entirely by its composition.</summary>
    TEndpoint RegisterEndpoint<TEndpoint>(Func<IContainerBuilder, TEndpoint> composeEndpoint) where TEndpoint : IEndpoint;

    ///<summary>The endpoints registered with this host so far, in registration order.</summary>
    IReadOnlyList<IEndpoint> Endpoints { get; }

    ///<summary>Starts every registered endpoint: all listening host-wide, then all address announcements, then all sending.</summary>
    Task StartAsync();

    ///<summary>Synchronous wrapper for <see cref="StartAsync"/>.</summary>
    void Start();
}
