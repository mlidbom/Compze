using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing.Wiring;

namespace Compze.Tessaging.InternalSpecifications;

///<summary>The surroundings a specification gives a production host it means to place in a topology of its own: the current
/// test's transport protocol and serializer, and announcing to — and discovering through — the registry the specification
/// created.</summary>
///<remarks>The registry is the real <see cref="InterprocessEndpointRegistry"/> rather than a stand-in, so a specification that
/// takes an endpoint out of the topology does it by retracting the announcement, which is the same act a departing process
/// performs. Specifications needing only one host use <c>TestingEndpointHost</c>, which brings its own registry; this exists for
/// the ones that need several hosts to find each other, where each host having its own registry would leave them blind.</remarks>
///<remarks>No domain database is bound: this environment hosts the database-less best-effort tier, and an exactly-once endpoint
/// built in it fails loud at the foundation assert naming the missing declaration.</remarks>
class EnvironmentParticipatingInTheSpecificationsRegistry : IEndpointEnvironment
{
   readonly InterprocessEndpointRegistry _registry;
   internal EnvironmentParticipatingInTheSpecificationsRegistry(InterprocessEndpointRegistry registry) => _registry = registry;

   public void Configure(EndpointBuilder endpointBuilder)
   {
      endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
      endpointBuilder.Serializer(registrar => registrar.CurrentTestsSerializersIfNotClonedContainer());
      endpointBuilder.ParticipateIn(_registry);
   }

   public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) {}
}
