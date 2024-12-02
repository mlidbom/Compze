using System.Threading.Tasks;
using AccountManagement.API;
using Compze.DependencyInjection;
using Compze.Messaging.Buses;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.Messaging.Buses;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement;

public class SetupTest([NotNull] string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   [Test] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create(TestingContainerFactory.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      host.RegisterTestingEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      await host.StartAsync().CaF();
   }
}
