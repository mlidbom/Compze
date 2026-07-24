using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Tests.Integration.Hosting;

///<summary>The <see cref="IEndpointEnvironment"/> the Hosting specifications give a production host<br/>
/// (<see cref="Compze.Hosting.EndpointHost.Production"/>): the current test's transport protocol and serializer, plus whichever
/// place in the topology the specification is putting this endpoint — named by the factory method it is created through.</summary>
///<remarks>Discovering and announcing are separate declarations because they are separate roles, and the specifications pin what
/// each one alone does: an endpoint that only announces is reachable but navigates nowhere, and navigating from it fails loud
/// naming the discovery declaration it never made.</remarks>
///<remarks>It deliberately binds no domain database: it hosts the database-less best-effort tier, and an exactly-once
/// endpoint built in it fails loud at the foundation assert naming the missing domain-database declaration.</remarks>
class CurrentTestsBestEffortEnvironment : IEndpointEnvironment
{
   readonly Action<EndpointBuilder> _declareItsPlaceInTheTopology;
   CurrentTestsBestEffortEnvironment(Action<EndpointBuilder> declareItsPlaceInTheTopology) => _declareItsPlaceInTheTopology = declareItsPlaceInTheTopology;

   ///<summary>The endpoint neither announces itself nor looks for anyone: it can only serve what reaches it.</summary>
   internal static CurrentTestsBestEffortEnvironment DeclaringNoTopology() => new(_ => {});

   ///<summary>The endpoint finds its peers through <paramref name="registry"/>, without announcing itself to it.</summary>
   internal static CurrentTestsBestEffortEnvironment DiscoveringEndpointsThrough(IEndpointRegistry registry) => new(it => it.DiscoverEndpointsThrough(registry));

   ///<summary>The endpoint announces where it listens to <paramref name="announcer"/>, without looking for anyone through it.</summary>
   internal static CurrentTestsBestEffortEnvironment AnnouncingItsAddressTo(IEndpointAddressAnnouncer announcer) => new(it => it.AnnounceAddressTo(announcer));

   ///<summary>The endpoint both announces itself to <paramref name="registry"/> and finds its peers through it.</summary>
   internal static CurrentTestsBestEffortEnvironment ParticipatingIn<TRegistry>(TRegistry registry) where TRegistry : IEndpointRegistry, IEndpointAddressAnnouncer =>
      new(it => it.ParticipateIn(registry));

   public void Configure(EndpointBuilder endpointBuilder)
   {
      endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
      endpointBuilder.Serializer(registrar => registrar.CurrentTestsSerializersIfNotClonedContainer());
      _declareItsPlaceInTheTopology(endpointBuilder);
   }

   public void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder) {} //Deliberately binds none: this environment hosts the database-less tier, and an exactly-once endpoint built in it fails loud at the foundation assert.
}
