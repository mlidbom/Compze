using System;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using Compze.Utilities.Testing.XUnit.ComponentPermutations;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Retry_policies_AtMostOnceCommand_when_command_handler_fails : EndpointHostTestBase
{
   public override async Task InitializeAsync()
   {
      await base.InitializeAsync();
      const string exceptionMessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
      MyCreateAggregateCommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionMessage));

      Host.AssertThatRunningScenarioThrowsBackendAndClientException<TransactionAbortedException>(action: () => ClientEndpoint.ExecuteClientRequest(navigator => navigator.Post(MyCreateAggregateCommand.Create())));
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