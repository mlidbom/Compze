using System;
using System.Transactions;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Utilities.SystemCE.TransactionsCE.Testing;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Threading.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Exactly_once_guarantee_tests : EndpointHostTestBase
{
   [PCT] public void If_transaction_fails_after_successfully_Sending_ExactlyOnceTommand_tommand_never_reaches_tommand_handler()
   {
      FluentActions.Invoking(() => RemoteEndpoint.ExecuteServerRequestInTransaction(session =>
                    {
                       Transaction.Current!.FailOnPrepare();
                       session.Send(new MyExactlyOnceTommand());
                    }))
                   .Should().Throw<TransactionAbortedException>();

      MyExactlyOnceTommandHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, 1.Seconds())
                              .Should()
                              .Be(false, "tommand should not reach handler");
   }

   [PCT] public void If_transaction_fails_after_successfully_Publishing_ExactlyOnceTevent_tevent_never_reaches_remote_handler_but_does_reach_local_handler()
   {
      const string exceptionTessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
      MyCreateTaggregateTommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionTessage));

      var (backendException, frontEndException) = Host.AssertThatRunningScenarioThrowsBackendAndClientException<TransactionAbortedException>(() => ClientEndpoint.ExecuteClientRequest(navigator => navigator.Post(MyCreateTaggregateTommand.Create())));

      backendException.InnerException!.Message.Should().Contain(exceptionTessage);
      frontEndException.Message.Should().Contain(exceptionTessage);

      MyLocalTaggregateTeventHandlerThreadGate.Passed.Should().BeGreaterThanOrEqualTo(1);

      MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, 1.Seconds())
                                             .Should()
                                             .Be(false, "tevent should not reach handler");
   }
}