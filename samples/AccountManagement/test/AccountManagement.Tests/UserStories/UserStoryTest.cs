using AccountManagement.UserStories.Scenarios;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Typermedia;
using Compze.Tests.Infrastructure;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase
{
   TestingEndpointHost Host { get; }
   readonly ExactlyOnceEndpoint _endpoint;
   TypermediaTestClient _client = null!;
   internal AccountScenarioApi Scenario => new(_client.Navigator);

   protected UserStoryTest()
   {
      Host = TestingEndpointHost.Create();
      _endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(Host);
   }

   protected override async Task InitializeAsyncInternal()
   {
      await Host.StartAsync().caf();
      //The statistics endpoint's query models update from the domain endpoint's tevents, and exactly-once fan-out membership
      //is the remembered subscribers - first contact is the boundary - so the user stories' immediate acts await the two
      //endpoints having met instead of racing the discovery.
      await Host.AwaitEndpointsHaveMetEachOtherAsync().caf();
      _client = await TypermediaTestClient.ConnectTo(_endpoint.Address!, registrar => registrar.RequireAccountManagementTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await Host.DisposeAsync().caf();
   }
}
