using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging.Endpoints.BestEffort;

///<summary>
/// The best-effort endpoint: the <see cref="Endpoint"/> whose TessageBus rung is best-effort. No database. Process-lifetime
/// peer memory; per-peer best-effort tevent queues that outlive connections (queue-while-down,
/// <see cref="EndpointBuilder{TConcreteBuilder}.RequirePeers"/> pens for peers that must be met before anything is dropped, a bound per peer,
/// <see cref="EndpointBuilder{TConcreteBuilder}.DoNotQueueTeventsFor"/> opt-down); arriving tessages dispatch through the engine's executor.
/// Serves all four tessage kinds unconditionally — but a handler for a tessage type whose declared contract demands
/// exactly-once delivery fails at composition: this endpoint has no inbox to persist, dedup, and retry with, and an endpoint
/// that cannot honor a guarantee must not advertise for it.
///
/// Composed through <see cref="Build"/>; with no durable state, the consistency law holds for it trivially — inline
/// handling <em>is</em> its consistency.
///</summary>
public class BestEffortEndpoint : Endpoint
{
   ///<summary>Composes a best-effort endpoint: runs <paramref name="build"/> over the endpoint's declaration surface<br/>
   /// (<see cref="BestEffortEndpointBuilder"/>), builds the endpoint's container, and returns the endpoint, ready for its<br/>
   /// lifecycle to be driven — directly, or by the <see cref="IEndpointHost"/> that owns it<br/>
   /// (<see cref="IEndpointHost.RegisterEndpoint{TEndpoint}"/>).</summary>
   public static BestEffortEndpoint Build(IContainerBuilder containerBuilder, string name, EndpointId id, Action<BestEffortEndpointBuilder> build)
   {
      var builder = new BestEffortEndpointBuilder(containerBuilder, new EndpointConfiguration(name, id));
      build(builder);
      return builder.Build();
   }

   internal BestEffortEndpoint(IDependencyInjectionContainer container,
                               EndpointConfiguration configuration,
                               IReadOnlyList<IEndpointAddressAnnouncer> addressAnnouncers,
                               IEndpointRegistry? endpointRegistry)
      : base(container, configuration, addressAnnouncers, endpointRegistry) {}
}
