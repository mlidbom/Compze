using Compze.Tessaging.Hosting;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Must;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

[LongRunning]
public class Outbox_retry_tests : EndpointHostTestBase
{
   [PCT]
   public async Task When_remote_endpoint_is_down_tessages_are_stored_and_delivered_after_endpoint_restarts()
   {
      await BackendEndPoint.StopListeningComponentsAsync();
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceTommand()));

      MyExactlyOnceTommandHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(1)).Must().BeFalse();

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
      await StartHostAsync();

      MyExactlyOnceTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }
}
