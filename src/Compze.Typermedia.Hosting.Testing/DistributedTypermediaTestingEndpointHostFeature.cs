using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.Testing;
using Compze.Typermedia.Client;
using Compze.Typermedia.Hosting.Testing.Wiring;

namespace Compze.Typermedia.Hosting.Testing;

///<summary>
/// Plugs distributed Typermedia into a <see cref="TestingEndpointHost"/>. Every endpoint the host registers gets
/// the distributed Typermedia pipeline (via <see cref="EndpointBuilderDistributedTypermediaExtensions.AddDistributedTypermedia(IEndpointBuilder)"/>) and the current
/// test's Typermedia transport. Typermedia has no background work, so the feature takes no part in the host's
/// dispose-time quiescence wait.
///</summary>
public class DistributedTypermediaTestingEndpointHostFeature : ITestingEndpointHostFeature
{
   public void SetupEndpoint(IEndpointBuilder builder)
   {
      builder.Registrar.CurrentTestsTypermediaTransport();
      builder.AddDistributedTypermedia();
   }
}
