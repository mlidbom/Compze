using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using System.Threading.Tasks;
using Compze.Threading.TasksCE;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase
{
   protected ITestingEndpointHost Host { get; set; }
   readonly IEndpoint _endpoint;
   IClient _client = null!;
   internal AccountScenarioApi Scenario => new(_client!);

   protected UserStoryTest()
   {
      Host = TestingEndpointHost.Create();
      _endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(Host);
   }

   protected override async Task InitializeAsyncInternal()
   {
      await Host.StartAsync().caf();
      _client = await TestClient.ConnectTo(_endpoint.Address!).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await Host.DisposeAsync().caf();
   }
}