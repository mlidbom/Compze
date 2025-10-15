using System.Threading.Tasks;
using AccountManagement.API;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Sql;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;
using Compze.Utilities.Threading.TasksCE;

namespace AccountManagement;

public class SetupTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   [Test] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create(runMode =>
      {
         var container = TestEnv.DIContainer.Create(runMode);
         container.Register().CurrentTestsDbPoolIfNotAlreadyRegistered();
         return container;
      });
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      host.RegisterClientEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      await host.StartAsync().caf();
      await host.DisposeAsync().caf();
   }
}
