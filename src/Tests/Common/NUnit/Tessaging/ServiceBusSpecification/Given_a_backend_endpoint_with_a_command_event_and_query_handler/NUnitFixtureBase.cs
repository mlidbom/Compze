using System.Threading.Tasks;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Tests.Infrastructure.NUnit;
using NUnit.Framework;

namespace Compze.Tests.Common.NUnit.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

[TestFixture, TestFixtureSource(typeof(PluggableComponentsTestFixtureSource))]
public abstract class NUnitEndpointHostTestBase(string _) : EndpointHostTestBase
{
   [SetUp] public override async Task SetupAsync() => await base.SetupAsync();
   [TearDown] public override async Task TearDownAsync() => await base.TearDownAsync();
}
