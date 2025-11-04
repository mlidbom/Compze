using System;
using Compze.Utilities.SystemCE;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE.ThreadingCE.Testing;
using Compze.Utilities.Testing.Must;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

[LongRunning]
public class Outbox_retry_tests : EndpointHostTestBase
{
   [PCT]
   public async Task When_remote_endpoint_is_down_tessages_are_stored_and_delivered_after_endpoint_restarts()
   {
      await BackendEndPoint.StopListeningComponentsAsync();
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceTommand()));

      var originalRemoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.ITessageStorage>();
      await Await.Async(() => originalRemoteStorage.GetUndeliveredTessages(TimeSpan.Zero).Any(it => it.RetryCount > 0),
                        10.Seconds(),
                        10.Milliseconds(),
                        "A tessage with a retry count greater than 0 should have been added to storage");

      var undeliveredTessage = originalRemoteStorage.GetUndeliveredTessages(TimeSpan.Zero)[0];
      undeliveredTessage.RetryCount.Must().BeGreaterThan(0, "failure should increment retry count");
      undeliveredTessage.LastAttemptTime.Must().NotBeNull();

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
      await StartHostAsync();

      var newRemoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.ITessageStorage>();

      MyExactlyOnceTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, 15.Seconds());
      await Await.Async(() => newRemoteStorage.GetUndeliveredTessages(TimeSpan.Zero).Count == 0,
                        10.Seconds(),
                        10.Milliseconds(),
                        "Timeout waiting for tessages to be removed from outbox");

      originalRemoteStorage.GetUndeliveredTessages(TimeSpan.Zero).Must().HaveCount(0, "the new endpoint after restart should be using the same database");
   }

   [PCT]
   public async Task Outbox_records_failure()
   {
      await BackendEndPoint.StopListeningComponentsAsync();
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceTommand()));

      var remoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.ITessageStorage>();
      await Await.Async(() => remoteStorage.GetUndeliveredTessages(TimeSpan.Zero).Any(it => it.RetryCount > 0),
                        10.Seconds(),
                        10.Milliseconds(),
                        "A tessage with a retry count greater than 0 should have been added to storage");

      var undeliveredTessage = remoteStorage.GetUndeliveredTessages(TimeSpan.Zero)[0];
      undeliveredTessage.RetryCount.Must().BeGreaterThan(0, "failure should increment retry count");
      undeliveredTessage.LastAttemptTime.Must().NotBeNull();

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
   }
}
