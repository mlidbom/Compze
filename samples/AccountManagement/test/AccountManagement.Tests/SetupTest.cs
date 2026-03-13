using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

namespace AccountManagement;

public class SetupTest : UniversalTestBase
{
   [PCT] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create();
      var endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(host);
      await host.StartAsync().caf();
      await using var client = await TestClient.ConnectTo(endpoint.TypermediaAddress!).caf();
      await host.DisposeAsync().caf();
   }
}
