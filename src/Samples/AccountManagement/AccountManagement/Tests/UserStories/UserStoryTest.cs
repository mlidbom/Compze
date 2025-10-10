using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.UserStories.Scenarios;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Infrastructure;
using Compze.Threading.TasksCE;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace AccountManagement.UserStories;

public abstract class UserStoryTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected ITestingEndpointHost? Host { get; set; }
   IEndpoint? _clientEndpoint;
   internal AccountScenarioApi Scenario => new(_clientEndpoint!);

   [SetUp] public async Task SetupContainerAndBeginScope()
   {
      Host = TestingEndpointHost.Create(TestingContainerFactory.Create);
      new AccountManagementServerDomainBootstrapper().RegisterWith(Host);
      _clientEndpoint = Host.RegisterClientEndpoint(setup:AccountApi.RegisterWithClientEndpoint);
      await Host.StartAsync().caf();
   }

   [TearDown] public async Task Teardown() => await Host!.DisposeAsync().caf();
}