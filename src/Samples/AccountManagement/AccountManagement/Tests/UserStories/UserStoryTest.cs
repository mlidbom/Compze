using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Threading.TasksCE;
using Xunit;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase, IAsyncLifetime
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

   public virtual async Task InitializeAsync() => await Host.StartAsync().caf();

   public async Task DisposeAsync()
   {
      await Host.DisposeAsync().caf();
      await _clientEndpoint.DisposeAsync();
   }
}