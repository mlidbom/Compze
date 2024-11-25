using System.Threading.Tasks;
using AccountManagement.API;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement;

public class SetupTest : DuplicateByPluggableComponentTest
{
   [Test] public async Task TestSetup()
   {
      var host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(host);
      host.RegisterTestingEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
      await host.StartAsync().CaF();
   }

   public SetupTest([NotNull] string pluggableComponentsColonSeparated) : base(pluggableComponentsColonSeparated) {}
}
