using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Tessaging.Endpoints;

///<summary>
/// A convenience owning several <see cref="IEndpoint"/>s' lifecycles in one process: starting the host starts every
/// endpoint, disposing it disposes them. Endpoints are first-class — each drives its own phase ordering
/// (<see cref="IEndpoint.StartAsync"/>), and the host adds nothing an endpoint cannot do alone. There is no ordering
/// between the endpoints: whether co-hosted endpoints have discovered each other when <see cref="StartAsync"/> completes is
/// topology convergence, exactly as it is between processes — an application awaits what it needs through readiness,
/// waiting sends absorb the churn, and queue-while-down and <c>RequirePeers</c> hold one-way tessages for peers not yet
/// met.
///
/// Production code creates a host via <c>EndpointHost.Production.Create</c> (in Compze.Hosting); tests use
/// the testing host in Compze.Tessaging.Hosting.Testing.
///</summary>
public interface IEndpointHost : IAsyncDisposable
{
    ///<summary>Registers the endpoint an <see cref="IExactlyOnceEndpointDeclaration"/> declares, built in this host's<br/>
    /// <see cref="IEndpointEnvironment"/> (<see cref="IExactlyOnceEndpointDeclaration.Build"/>) — the declaration brings what<br/>
    /// the endpoint is, the host brings where it runs.</summary>
    ExactlyOnceEndpoint RegisterEndpoint(IExactlyOnceEndpointDeclaration declaration);

    ///<summary>Registers the endpoint an <see cref="IBestEffortEndpointDeclaration"/> declares, built in this host's<br/>
    /// <see cref="IEndpointEnvironment"/> (<see cref="IBestEffortEndpointDeclaration.Build"/>).</summary>
    BestEffortEndpoint RegisterEndpoint(IBestEffortEndpointDeclaration declaration);

    ///<summary>Registers the endpoint an <see cref="IExactlyOnceEndpointDeclaration"/> declares, built in<br/>
    /// <paramref name="environment"/> instead of this host's own — the composition for an endpoint whose environment differs<br/>
    /// from its co-hosted neighbors', usually a decorating <see cref="IEndpointEnvironment"/> wrapping the host's with an<br/>
    /// addition.</summary>
    ExactlyOnceEndpoint RegisterEndpoint(IExactlyOnceEndpointDeclaration declaration, IEndpointEnvironment environment);

    ///<summary>Registers the endpoint an <see cref="IBestEffortEndpointDeclaration"/> declares, built in <paramref name="environment"/> instead of this host's own.</summary>
    BestEffortEndpoint RegisterEndpoint(IBestEffortEndpointDeclaration declaration, IEndpointEnvironment environment);

    ///<summary>The endpoints registered with this host so far, in registration order.</summary>
    IReadOnlyList<IEndpoint> Endpoints { get; }

    ///<summary>Starts every registered endpoint (<see cref="IEndpoint.StartAsync"/>), each driving its own phase ordering; completes when all have started.</summary>
    Task StartAsync();

    ///<summary>Synchronous wrapper for <see cref="StartAsync"/>.</summary>
    void Start();
}
