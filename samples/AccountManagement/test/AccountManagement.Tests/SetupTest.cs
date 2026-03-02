using System.Threading.Tasks;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading.TasksCE;

namespace AccountManagement;

public class SetupTest : UniversalTestBase
{
   [PCT] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create();
      var endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(host);
      await host.StartAsync().caf();
      await using var client = await TestClient.ConnectTo(endpoint.Address!).caf();
      await host.DisposeAsync().caf();
   }
}
