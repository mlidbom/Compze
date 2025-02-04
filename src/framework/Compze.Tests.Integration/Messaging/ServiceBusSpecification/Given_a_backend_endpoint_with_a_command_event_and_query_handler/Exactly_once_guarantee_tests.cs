﻿using System;
using System.Transactions;
using Compze.Messaging.Buses;
using Compze.Testing.Messaging.Buses;
using Compze.Testing.Threading;
using Compze.Testing.Transactions;
using Compze.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace Compze.Tests.Integration.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler;

public class Exactly_once_guarantee_tests(string pluggableComponentsCombination) : Fixture(pluggableComponentsCombination)
{
   [Test] public void If_transaction_fails_after_successfully_Sending_ExactlyOnceCommand_command_never_reaches_command_handler()
   {
      FluentActions.Invoking(() => RemoteEndpoint.ExecuteServerRequestInTransaction(session =>
                    {
                       Transaction.Current!.FailOnPrepare();
                       session.Send(new MyExactlyOnceCommand());
                    }))
                   .Should().Throw<TransactionAbortedException>();

      CommandHandlerThreadGate.TryAwaitPassededThroughCountEqualTo(1, 1.Seconds())
                              .Should()
                              .Be(false, "command should not reach handler");
   }

   [Test] public void If_transaction_fails_after_successfully_Publishing_ExactlyOnceEvent_event_never_reaches_remote_handler_but_does_reach_local_handler()
   {
      const string exceptionMessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
      MyCreateAggregateCommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionMessage));

      var (backendException, frontEndException) = Host.AssertThatRunningScenarioThrowsBackendAndClientException<TransactionAbortedException>(() => ClientEndpoint.ExecuteClientRequest(navigator => navigator.Post(MyCreateAggregateCommand.Create())));

      backendException.InnerException!.Message.Should().Contain(exceptionMessage);
      frontEndException.Message.Should().Contain(exceptionMessage);

      MyLocalAggregateEventHandlerThreadGate.Passed.Should().BeGreaterOrEqualTo(1);

      MyRemoteAggregateEventHandlerThreadGate.TryAwaitPassededThroughCountEqualTo(1, 1.Seconds())
                                             .Should()
                                             .Be(false, "event should not reach handler");
   }
}