﻿using System;
using System.Transactions;
using Compze.Messaging.Buses;
using Compze.Testing.Messaging.Buses;
using Compze.Testing.Threading;
using Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Compze.Tests.Integration.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Retry_policies_AtMostOnceCommand_when_command_handler_fails(string pluggableComponentsCombination) : Fixture(pluggableComponentsCombination)
{
   [SetUp] public void SendCommandThatFails()
   {
      const string exceptionMessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
      MyCreateAggregateCommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionMessage));

      Host.AssertThatRunningScenarioThrowsBackendAndClientException<TransactionAbortedException>(action: () => ClientEndpoint.ExecuteClientRequest(navigator => navigator.Post(MyCreateAggregateCommand.Create())));
   }

   [Test] public void ExactlyOnce_Event_raised_in_handler_does_not_reach_remote_handler()
   {
      MyRemoteAggregateEventHandlerThreadGate.TryAwaitPassededThroughCountEqualTo(count: 1, 1.Seconds())
                                             .Should()
                                             .Be(expected: false, because: "event should not reach handler");
   }

   [Test] public void Command_handler_is_tried_5_times() => MyCreateAggregateCommandHandlerThreadGate.Passed.Should().Be(expected: 5);

   [Test] public void ExactlyOnce_Event_raised_in_handler_reaches_local_handler_5_times() => MyLocalAggregateEventHandlerThreadGate.Passed.Should().Be(expected: 5);
}