using Compze.Hosting.Testing.Wiring;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Tests.Integration.Hosting;

///<summary>The <see cref="IEndpointEnvironment"/> the Hosting specifications give a production host<br/>
/// (<see cref="Compze.Hosting.EndpointHost.Production"/>): the current test's transport protocol, Newtonsoft serialization,<br/>
/// and — when created with a registry — discovery through it; created without one, the endpoint discovers nothing and only<br/>
/// serves. It deliberately binds no domain database: it hosts the database-less best-effort tier, and an exactly-once<br/>
/// endpoint built in it fails loud at the foundation assert naming the missing domain-database declaration.</summary>
class CurrentTestsBestEffortEnvironment : IEndpointEnvironment
{
   readonly IEndpointRegistry? _discoverEndpointsThrough;

   internal CurrentTestsBestEffortEnvironment(IEndpointRegistry? discoverEndpointsThrough = null) => _discoverEndpointsThrough = discoverEndpointsThrough;

   public void DeclareOn(EndpointBuilder endpointBuilder)
   {
      endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
      endpointBuilder.NewtonsoftSerializer();
      if(_discoverEndpointsThrough is not null) endpointBuilder.DiscoverEndpointsThrough(_discoverEndpointsThrough);
   }

   public void DeclareDomainDatabaseOn(ExactlyOnceEndpointBuilder endpointBuilder) {} //Deliberately binds none: this environment hosts the database-less tier, and an exactly-once endpoint built in it fails loud at the foundation assert.
}
