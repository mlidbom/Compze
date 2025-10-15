using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;
using Compze.Utilities.Threading.TasksCE;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected ITestingEndpointHost? Host { get; set; }
   IEndpoint? _clientEndpoint;
   internal AccountScenarioApi Scenario => new(_clientEndpoint!);

   [SetUp] public async Task SetupContainerAndBeginScope()
   {
      Host = TestingEndpointHost.Create(runMode => TestEnv.DIContainer.Create(runMode));
      new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
      _clientEndpoint = Host.RegisterClientEndpoint(setup:AccountApi.RegisterWithClientEndpoint);
      await Host.StartAsync().caf();
   }

   [TearDown] public async Task Teardown() => await Host!.DisposeAsync().caf();
}