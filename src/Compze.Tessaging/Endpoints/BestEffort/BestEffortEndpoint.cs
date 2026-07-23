using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Endpoints.BestEffort;

///<summary>
/// The best-effort endpoint: the <see cref="Endpoint"/> whose TessageBus rung is best-effort. No database. Process-lifetime
/// peer memory; per-peer best-effort tevent queues that outlive connections (queue-while-down,
/// <see cref="EndpointBuilder.RequirePeers"/> pens for peers that must be met before anything is dropped, a bound per peer,
/// <see cref="EndpointBuilder.DoNotQueueTeventsFor"/> opt-down); arriving tessages dispatch through the engine's executor.
/// Serves all four tessage kinds unconditionally — but a handler for a tessage type whose declared contract demands
/// exactly-once delivery fails at composition: this endpoint has no inbox to persist, dedup, and retry with, and an endpoint
/// that cannot honor a guarantee must not advertise for it.
///
/// Built from a <see cref="BestEffortEndpointDeclaration{TIdentity}"/>
/// (<see cref="IBestEffortEndpointDeclaration.Build"/>); with no durable state, the consistency law holds for it
/// trivially — inline handling <em>is</em> its consistency.
///</summary>
public class BestEffortEndpoint : Endpoint
{
   internal BestEffortEndpoint(IDependencyInjectionContainer container,
                               EndpointConfiguration configuration,
                               IReadOnlyList<IEndpointAddressAnnouncer> addressAnnouncers,
                               IEndpointRegistry? endpointRegistry)
      : base(container, configuration, addressAnnouncers, endpointRegistry) {}
}
