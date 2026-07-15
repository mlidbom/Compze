using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Typermedia.Client;

namespace Compze.Typermedia.Hosting.Testing;

///<summary>
/// Plugs distributed Typermedia into a <see cref="TestingEndpointHost"/>. Every endpoint the host registers gets
/// the distributed Typermedia pipeline (via <see cref="EndpointBuilderDistributedTypermediaExtensions.AddDistributedTypermedia(IEndpointBuilder)"/>),
/// the endpoint transport of the current test's protocol, and an <see cref="IEndpointRegistry"/> listing the
/// host's typermedia addresses — so every endpoint's typermedia router connects to every endpoint in the host,
/// and endpoints navigate each other's typermedia exactly as they would across processes. Typermedia has no
/// background work, so the feature takes no part in the host's dispose-time quiescence wait.
///</summary>
public class DistributedTypermediaTestingEndpointHostFeature : ITestingEndpointHostFeature
{
   ITestingEndpointHost? _host;

   public void OnAddedToHost(ITestingEndpointHost host) => _host = host;

   public void SetupEndpoint(IEndpointBuilder builder)
   {
      builder.Registrar.CurrentTestsEndpointTransport();
      builder.AddDistributedTypermedia()
             .DiscoverEndpointsThrough(new TestingHostEndpointRegistry(() => _host._assert().NotNull().Endpoints));
   }

   ///<summary>Knows the typermedia addresses of every endpoint in the testing host, so that typermedia routers connect to all of them.</summary>
   class TestingHostEndpointRegistry(Func<IReadOnlyList<IEndpoint>> hostEndpoints) : IEndpointRegistry
   {
      readonly Func<IReadOnlyList<IEndpoint>> _hostEndpoints = hostEndpoints;

      public IEnumerable<EndpointAddress> ServerEndpointAddresses => [.._hostEndpoints().Where(it => it.TypermediaAddress is not null)
                                                                                        .Select(it => it.TypermediaAddress!)];
   }
}
