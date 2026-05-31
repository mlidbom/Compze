using AccountManagement.UserStories.Scenarios;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Hosting.Testing.Tessaging;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase
{
   ITestingEndpointHost Host { get; set; }
   readonly IEndpoint _endpoint;
   TestClient _client = null!;
   internal AccountScenarioApi Scenario => new(_client.Navigator);

   protected UserStoryTest()
   {
      Host = TestingEndpointHost.Create();
      _endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(Host);
   }

   protected override async Task InitializeAsyncInternal()
   {
      await Host.StartAsync().caf();
      _client = await TestClient.ConnectTo(_endpoint.TypermediaAddress!, mapper => mapper.RegisterAccountManagementTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await Host.DisposeAsync().caf();
   }
}