using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.DependencyInjection;
using Compze.Messaging.Buses;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using Compze.Testing.Messaging.Buses;
using JetBrains.Annotations;
using NUnit.Framework;

namespace AccountManagement.UserStories;

public class UserStoryTest([NotNull] string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected ITestingEndpointHost Host { get; set; }
   IEndpoint _clientEndpoint;
   internal AccountScenarioApi Scenario => new(_clientEndpoint);

   [SetUp] public async Task SetupContainerAndBeginScope()
   {
      Host = TestingEndpointHost.Create(DependencyInjectionContainer.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
      _clientEndpoint = Host.RegisterTestingEndpoint(setup:AccountApi.RegisterWithClientEndpoint);
      await Host.StartAsync().CaF();
   }

   [TearDown] public async Task Teardown() => await Host.DisposeAsync().CaF();
}