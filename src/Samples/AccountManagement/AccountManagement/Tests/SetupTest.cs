using System.Threading.Tasks;
using AccountManagement.API;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Threading.TasksCE;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace AccountManagement;

public class SetupTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   [Test] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create(TestingContainerFactory.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      host.RegisterClientEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      await host.StartAsync().caf();
      await host.DisposeAsync().caf();
   }
}
