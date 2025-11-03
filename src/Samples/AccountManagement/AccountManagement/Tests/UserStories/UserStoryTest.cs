using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase
{
   protected ITestingEndpointHost Host { get; set; }
   readonly IEndpoint _clientEndpoint;
   internal AccountScenarioApi Scenario => new(_clientEndpoint!);

   protected UserStoryTest()
   {
      Host = TestingEndpointHost.Create();
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