using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.Testing;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement.UserStories;

public class UserStoryTest : DuplicateByPluggableComponentTest
{
   protected ITestingEndpointHost Host { get; set; }
   IEndpoint _clientEndpoint;
   internal AccountScenarioApi Scenario => new(_clientEndpoint);

   [SetUp] public async Task SetupContainerAndBeginScope()
   {
      Host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
      _clientEndpoint = Host.RegisterTestingEndpoint(setup:AccountApi.RegisterWithClientEndpoint);
      await Host.StartAsync().NoMarshalling();
   }

   [TearDown] public async Task Teardown() => await Host.DisposeAsync().NoMarshalling();

   public UserStoryTest([NotNull] string _) : base(_) {}
}