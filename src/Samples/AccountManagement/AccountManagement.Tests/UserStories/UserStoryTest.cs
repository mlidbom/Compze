using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.Tessaging.Buses;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.Persistence;
using Compze.Testing.Tessaging.Buses;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using NUnit.Framework;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected ITestingEndpointHost? Host { get; set; }
   IEndpoint? _clientEndpoint;
   internal AccountScenarioApi Scenario => new(_clientEndpoint!);

   [SetUp] public async Task SetupContainerAndBeginScope()
   {
      Host = TestingEndpointHost.Create(TestingContainerFactory.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(
         Host,
         configurePersistence: builder => builder.RegisterCurrentTestsConfiguredPersistenceLayer());
      _clientEndpoint = Host.RegisterTestingEndpoint(setup:AccountApi.RegisterWithClientEndpoint);
      await Host.StartAsync().caf();
   }

   [TearDown] public async Task Teardown() => await Host!.DisposeAsync().caf();
}