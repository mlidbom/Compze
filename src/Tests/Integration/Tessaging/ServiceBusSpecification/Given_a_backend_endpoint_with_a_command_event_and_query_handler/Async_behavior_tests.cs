using System.Threading.Tasks;
using Compze.Tessaging.Typermedia.Abstractions;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Async_behavior_test : EndpointHostTestBase
{
   [PCT] public async Task Query_returns_task_immediately_does_not_block_until_awaited()
   {
      QueryHandlerThreadGate.Close();

      using var _ = ClientEndpoint.ServiceLocator.BeginScope();
      var session = ClientEndpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>();
      var query = session.GetAsync(new MyQuery());
      QueryHandlerThreadGate.Open();
      await query;
   }
}
