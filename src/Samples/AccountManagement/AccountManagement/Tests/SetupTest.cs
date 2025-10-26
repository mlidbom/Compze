using System.Threading.Tasks;
using AccountManagement.API;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Threading.TasksCE;

namespace AccountManagement;

public class SetupTest : UniversalTestBase
{
   [PCT] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create(registrar =>
      {
         var container = TestEnv.DIContainer.CreateWithServiceLocatorAndSerializer();
         container.Register()
                  .CurrentTestsConfiguredSqlLayer()
                  .CurrentTestsTransport();
         return container;
      });
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      host.RegisterClientEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      await host.StartAsync().caf();
      await host.DisposeAsync().caf();
   }
}
