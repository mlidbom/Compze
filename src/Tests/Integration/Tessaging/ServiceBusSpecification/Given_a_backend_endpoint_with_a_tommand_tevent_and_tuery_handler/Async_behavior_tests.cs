using System.Threading.Tasks;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Async_behavior_test : EndpointHostTestBase
{
   [PCT] public async Task Tuery_returns_task_immediately_does_not_block_until_awaited()
   {
      TueryHandlerThreadGate.Close();

      using var _ = ClientEndpoint.ServiceLocator.BeginScope();
      var session = ClientEndpoint.ServiceLocator.Resolve<IRemoteTypermediaNavigator>();
      var tuery = session.GetAsync(new MyTuery());
      TueryHandlerThreadGate.Open();
      await tuery;
   }
}
