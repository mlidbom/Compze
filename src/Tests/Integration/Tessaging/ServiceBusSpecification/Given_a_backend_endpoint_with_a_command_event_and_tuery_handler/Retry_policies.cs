using System;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_tuery_handler;

public class Retry_policies_AtMostOnceCommand_when_command_handler_fails : EndpointHostTestBase
{
   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      const string exceptionTessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
      MyCreateAggregateCommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionTessage));

      Host.AssertThatRunningScenarioThrowsBackendAndClientException<TransactionAbortedException>(action: () => ClientEndpoint.ExecuteClientRequest(navigator => navigator.Post(MyCreateAggregateTommand.Create())));
      await Task.CompletedTask;
   }

   [PCT] public void ExactlyOnce_Event_raised_in_handler_does_not_reach_remote_handler()
   {
      MyRemoteAggregateEventHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(count: 1, 1.Seconds())
                                             .Should()
                                             .Be(expected: false, because: "event should not reach handler");
   }

   [PCT] public void Command_handler_is_tried_5_times() => MyCreateAggregateCommandHandlerThreadGate.Passed.Should().Be(expected: 5);

   [PCT] public void ExactlyOnce_Event_raised_in_handler_reaches_local_handler_5_times() => MyLocalAggregateEventHandlerThreadGate.Passed.Should().Be(expected: 5);
}