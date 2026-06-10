using Compze.Tessaging.Hosting.AspNetCore.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing.Wiring;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarTessagingTransport
{
   ///<summary>Registers the Tessaging transport — inbox server, controller and transport client — plus the shared infrastructure transport if no other paradigm registered it yet.</summary>
   public static IComponentRegistrar CurrentTestsTessagingTransport(this IComponentRegistrar register) =>
      register.CurrentTestsInfrastructureTransportIfNotRegistered()
              .AspNetCoreTessagingTransport();
}
