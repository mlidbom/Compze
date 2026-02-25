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
   readonly IClient _client;
   internal AccountScenarioApi Scenario => new(_client!);

   protected UserStoryTest()
   {
      Host = TestingEndpointHost.Create();
      new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
      _client = Host.RegisterClient(setup:AccountApi.RegisterWithClientEndpoint);
   }

   protected override async Task InitializeAsyncInternal() => await Host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal()
   {
      await Host.DisposeAsync().caf();
      await _client.DisposeAsync();
   }
}