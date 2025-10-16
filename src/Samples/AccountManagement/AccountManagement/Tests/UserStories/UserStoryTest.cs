using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Threading.TasksCE;
using Xunit;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase, IAsyncLifetime
{
   protected ITestingEndpointHost? Host { get; set; }
   IEndpoint? _clientEndpoint;
   internal AccountScenarioApi Scenario => new(_clientEndpoint!);

   public virtual async Task InitializeAsync()
   {
      Host = TestingEndpointHost.Create(runMode => TestEnv.DIContainer.CreateWithRegisteredServiceLocator(runMode));
      new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
      _clientEndpoint = Host.RegisterClientEndpoint(setup:AccountApi.RegisterWithClientEndpoint);
      await Host.StartAsync().caf();
   }

   public async Task DisposeAsync() => await Host!.DisposeAsync().caf();
}