using System;
using System.Linq;
using System.Threading.Tasks;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tests.Common.NUnit.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Outbox_retry_tests(string pluggableComponentsCombination) : NUnitFixtureBase(pluggableComponentsCombination)
{
   [Test]
   public async Task When_remote_endpoint_is_down_messages_are_stored_and_delivered_after_endpoint_restarts()
   {
      await BackendEndPoint.StopAsync();
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

      var remoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.IMessageStorage>();
      await FluentActions.Awaiting(async () =>
                          {
                             var deadline = DateTime.UtcNow.Add(10.Seconds());
                             while(DateTime.UtcNow < deadline)
                             {
                                var messages = remoteStorage.GetUndeliveredMessages(TimeSpan.Zero);
                                if(messages.Count > 0 && messages.Any(m => m.RetryCount > 0))
                                   return;
                                await Task.Delay(100);
                             }

                             throw new Exception("Timeout waiting for messages to be recorded as undelivered");
                          })
                         .Should().NotThrowAsync();

      var undeliveredMessage = remoteStorage.GetUndeliveredMessages(TimeSpan.Zero)[0];
      undeliveredMessage.RetryCount.Should().BeGreaterThan(0, "failure should increment retry count");
      undeliveredMessage.LastAttemptTime.Should().NotBeNull("last attempt time should be recorded");

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
      await StartHostAsync();

      remoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.IMessageStorage>();

      Console.WriteLine("################### After restart");
      MyLocalAggregateEventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, 5.Seconds());
      await FluentActions.Awaiting(async () =>
                          {
                             var deadline = DateTime.UtcNow.Add(5.Seconds());
                             while(DateTime.UtcNow < deadline)
                             {
                                var undeliveredMessages = remoteStorage.GetUndeliveredMessages(TimeSpan.Zero);
                                undeliveredMessages.ForEach(it => Console.WriteLine($"########### Found undelivered: {it.MessageId}"));
                                if(undeliveredMessages.Count == 0)
                                   return;
                                await Task.Delay(100);
                             }

                             throw new Exception("Timeout waiting for messages to be removed from outbox");
                          })
                         .Should().NotThrowAsync();
   }

   [Test]
   public async Task Outbox_records_detailed_failure_information2()
   {
      await BackendEndPoint.StopAsync();
      RemoteEndpoint.ExecuteServerRequestInTransaction(session => session.Send(new MyExactlyOnceCommand()));

      var remoteStorage = RemoteEndpoint.ServiceLocator.Resolve<Outbox.IMessageStorage>();
      await FluentActions.Awaiting(async () =>
                          {
                             var deadline = DateTime.UtcNow.Add(10.Seconds());
                             while(DateTime.UtcNow < deadline)
                             {
                                var messages = remoteStorage.GetUndeliveredMessages(TimeSpan.Zero);
                                if(messages.Count > 0 && messages.Any(m => m.RetryCount > 0))
                                   return;
                                await Task.Delay(100);
                             }

                             throw new Exception("Timeout waiting for messages to be recorded as undelivered");
                          })
                         .Should().NotThrowAsync();

      var undeliveredMessage = remoteStorage.GetUndeliveredMessages(TimeSpan.Zero)[0];
      undeliveredMessage.RetryCount.Should().BeGreaterThan(0, "failure should increment retry count");
      undeliveredMessage.LastAttemptTime.Should().NotBeNull("last attempt time should be recorded");

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
      await StartHostAsync();
   }
}
