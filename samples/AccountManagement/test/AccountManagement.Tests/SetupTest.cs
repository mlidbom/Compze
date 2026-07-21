using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static AccountManagement.AccountManagementServerDomainBootstrapper;

namespace AccountManagement;

public class SetupTest : UniversalTestBase
{
   [PCT] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create();
      var endpoint = host.RegisterExactlyOnceEndpoint(DomainEndpointName, DomainEndpointId, DeclareDomainEndpoint);
      host.RegisterExactlyOnceEndpoint(StatisticsEndpointName, StatisticsEndpointId, DeclareStatisticsEndpoint);
      await host.StartAsync().caf();
      await using var client = await TypermediaTestClient.ConnectTo(endpoint.Address!, registrar => registrar.RequireAccountManagementTypeMappings()).caf();
      await host.DisposeAsync().caf();
   }
}
