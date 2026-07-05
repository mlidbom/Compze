using AccountManagement.UserStories.Scenarios;
using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.Testing;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.ServiceBus.Hosting.Testing;
using Compze.Typermedia.Client;
using Compze.Typermedia.Hosting.Testing;
using Compze.Tests.Infrastructure;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest : UniversalTestBase
{
   ITestingEndpointHost Host { get; set; }
   readonly IEndpoint _endpoint;
   TypermediaTestClient _client = null!;
   internal AccountScenarioApi Scenario => new(_client.Navigator);

   protected UserStoryTest()
   {
      Host = TestingEndpointHost.Create(new TessagingTestingEndpointHostFeature(), new TypermediaTestingEndpointHostFeature());
      _endpoint = AccountManagementServerDomainBootstrapper.RegisterWith(Host);
   }

   protected override async Task InitializeAsyncInternal()
   {
      await Host.StartAsync().caf();
      _client = await TypermediaTestClient.ConnectTo(_endpoint.TypermediaAddress!, mapper => mapper.RegisterAccountManagementTypeMappings()).caf();
   }

   protected override async Task DisposeAsyncInternal()
   {
      await _client.DisposeAsync().caf();
      await Host.DisposeAsync().caf();
   }
}
