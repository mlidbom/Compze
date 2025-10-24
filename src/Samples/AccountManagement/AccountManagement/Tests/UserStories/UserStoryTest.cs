using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Threading.TasksCE;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase
{
   protected ITestingEndpointHost Host { get; set; }
   readonly IEndpoint _clientEndpoint;
   internal AccountScenarioApi Scenario => new(_clientEndpoint!);

   protected UserStoryTest()
   {
      Host = TestingEndpointHost.Create(runMode => TestEnv.DIContainer.CreateWithRegisteredServiceLocator());
      new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
      _clientEndpoint = Host.RegisterClientEndpoint(setup:AccountApi.RegisterWithClientEndpoint);
   }

   protected override async Task InitializeAsyncInternal() => await Host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal()
   {
      await Host.DisposeAsync().caf();
      await _clientEndpoint.DisposeAsync();
   }
}