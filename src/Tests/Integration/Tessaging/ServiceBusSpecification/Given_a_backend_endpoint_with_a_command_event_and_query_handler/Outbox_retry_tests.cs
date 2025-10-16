using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Outbox_retry_tests : XUnitEndpointHostTestBase
{
   [PCT]
   public async Task When_remote_endpoint_is_down_messages_are_stored_and_delivered_after_endpoint_restarts()
   {
      await BackendEndPoint.StopAsync();
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

      var originalRemoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.IMessageStorage>();
      await Await.Async(() => originalRemoteStorage.GetUndeliveredMessages(TimeSpan.Zero).Any(it => it.RetryCount > 0),
                        10.Seconds(),
                        10.Milliseconds(),
                        "A message with a retry count greater than 0 should have been added to storage");

      var undeliveredMessage = originalRemoteStorage.GetUndeliveredMessages(TimeSpan.Zero)[0];
      undeliveredMessage.RetryCount.Should().BeGreaterThan(0, "failure should increment retry count");
      undeliveredMessage.LastAttemptTime.Should().NotBeNull("last attempt time should be recorded");

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
      await StartHostAsync();

      var newRemoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.IMessageStorage>();

      MyExactlyOnceCommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, 5.Seconds());
      await Await.Async(() => newRemoteStorage.GetUndeliveredMessages(TimeSpan.Zero).Count == 0,
                        10.Seconds(),
                        10.Milliseconds(),
                        "Timeout waiting for messages to be removed from outbox");

      originalRemoteStorage.GetUndeliveredMessages(TimeSpan.Zero).Should().HaveCount(0, "the new endpoint after restart should be using the same database");
   }

   [PCT]
   public async Task Outbox_records_detailed_failure_information2()
   {
      await BackendEndPoint.StopAsync();
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

      var remoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.IMessageStorage>();
      await Await.Async(() => remoteStorage.GetUndeliveredMessages(TimeSpan.Zero).Any(it => it.RetryCount > 0),
                        10.Seconds(),
                        10.Milliseconds(),
                        "A message with a retry count greater than 0 should have been added to storage");

      var undeliveredMessage = remoteStorage.GetUndeliveredMessages(TimeSpan.Zero)[0];
      undeliveredMessage.RetryCount.Should().BeGreaterThan(0, "failure should increment retry count");
      undeliveredMessage.LastAttemptTime.Should().NotBeNull("last attempt time should be recorded");

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
   }
}
