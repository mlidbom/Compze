using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Client;

namespace Compze.Tessaging.Hosting.Testing.Typermedia;

///<summary>
/// Plugs distributed Typermedia into a <see cref="TestingEndpointHost"/>. Every endpoint the host registers gets
/// the distributed Typermedia pipeline (via <see cref="EndpointBuilderDistributedTypermediaExtensions.AddDistributedTypermedia(IEndpointBuilder)"/>),
/// the endpoint transport of the current test's protocol, and participation in the host's endpoint registry
/// (<see cref="ITestingEndpointHost.EndpointRegistry"/> — every endpoint announces itself and discovers the
/// others through the same announce/discover pipeline a production same-machine suite runs) — so every
/// endpoint's typermedia router connects to every endpoint in the host, and endpoints navigate each other's
/// typermedia exactly as they would across processes. Typermedia has no background work, so the feature takes
/// no part in the host's dispose-time quiescence wait.
///</summary>
public class DistributedTypermediaTestingEndpointHostFeature : ITestingEndpointHostFeature
{
   ITestingEndpointHost? _host;

   public void OnAddedToHost(ITestingEndpointHost host) => _host = host;

   public void SetupEndpoint(IEndpointBuilder builder)
   {
      builder.Registrar.CurrentTestsEndpointTransport();
      builder.AddDistributedTypermedia()
             .ParticipateIn(_host._assert().NotNull().EndpointRegistry);
   }
}
