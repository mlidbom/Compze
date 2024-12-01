using System.Threading.Tasks;
using AccountManagement.API;
using Compze.DependencyInjection;
using Compze.Messaging.Buses;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement;

public class SetupTest([NotNull] string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   [Test] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      host.RegisterTestingEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      await host.StartAsync().CaF();
   }
}
