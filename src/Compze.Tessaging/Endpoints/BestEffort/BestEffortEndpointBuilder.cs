using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.Engine.HandlerRegistration.Internal;

namespace Compze.Tessaging.Endpoints.BestEffort;

///<summary>The declaration surface a <see cref="BestEffortEndpoint"/> is composed through — see <see cref="EndpointBuilder{TConcreteBuilder}"/>.<br/>
/// The best-effort tier declares no database: it persists nothing, which is exactly what makes it the tier with zero<br/>
/// operational ceremony.</summary>
public sealed class BestEffortEndpointBuilder : EndpointBuilder<BestEffortEndpointBuilder>
{
   internal BestEffortEndpointBuilder(IContainerBuilder containerBuilder, EndpointConfiguration configuration) : base(containerBuilder, configuration) {}

   internal BestEffortEndpoint Build() =>
      BuildEndpoint(container => new BestEffortEndpoint(container, Configuration, AddressAnnouncers, EndpointRegistry));

   ///<summary>The wiring rule (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>): a handler demanding more than<br/>
   /// the endpoint delivers fails at composition. This endpoint has no inbox to persist, dedup, and retry with, so a handler<br/>
   /// for a tessage type whose declared contract demands exactly-once delivery could never be honored: advertising it would<br/>
   /// pull exactly-once traffic the endpoint must refuse, stalling every sender's in-order delivery to it — and silently<br/>
   /// downgrading the guarantee instead is data loss dressed as success.</summary>
   private protected override void AssertTheRosterIsSound(TessageHandlerRoster roster)
   {
      var typesDemandingExactlyOnceDelivery = roster.RegisteredTypesDemandingExactlyOnceDelivery();
      State.Assert(typesDemandingExactlyOnceDelivery.Count == 0,
                   () => $"This best-effort endpoint wires no exactly-once delivery machinery — no inbox to persist, dedup, and retry with — but handlers are registered for tessage types whose declared contract demands it: {string.Join(", ", typesDemandingExactlyOnceDelivery.Select(it => it.FullName))}. A subscription takes the tessage type's full declared guarantee (observation included: observing a remote exactly-once tevent still requires receiving it exactly-once), and an endpoint that cannot honor a guarantee must not advertise for it. Compose an exactly-once endpoint instead, or handle tessage types that declare no exactly-once contract.");

      base.AssertTheRosterIsSound(roster);
   }
}
