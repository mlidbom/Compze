using System.Threading.Tasks;
using AccountManagement.API;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.Messaging.Buses;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using NUnit.Framework;

namespace AccountManagement;

public class SetupTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   [Test] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create(TestingContainerFactory.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      host.RegisterTestingEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      await host.StartAsync().caf();
      await host.DisposeAsync().caf();
   }
}
