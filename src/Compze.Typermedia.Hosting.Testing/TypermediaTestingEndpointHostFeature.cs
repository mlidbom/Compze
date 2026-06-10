using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.Testing;
using Compze.Typermedia.Client;
using Compze.Typermedia.Hosting.Testing.Wiring;

namespace Compze.Typermedia.Hosting.Testing;

///<summary>
/// Plugs Typermedia into a <see cref="TestingEndpointHost"/>. Every endpoint the host registers gets
/// the Typermedia pipeline (via <see cref="EndpointBuilderTypermediaExtensions.AddTypermedia"/>) and the current
/// test's Typermedia transport. Typermedia has no background work, so the feature takes no part in the host's
/// dispose-time quiescence wait.
///</summary>
public class TypermediaTestingEndpointHostFeature : ITestingEndpointHostFeature
{
   public void SetupEndpoint(IEndpointBuilder builder)
   {
      builder.Registrar.CurrentTestsTypermediaTransport();
      builder.AddTypermedia();
   }
}
