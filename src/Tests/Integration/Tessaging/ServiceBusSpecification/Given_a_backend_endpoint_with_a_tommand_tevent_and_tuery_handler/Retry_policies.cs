using System;
using Compze.Utilities.SystemCE;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Testing.Tessaging.Buses;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.Threading.Testing;
using Compze.Tests.Infrastructure.Fluent;


namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Retry_policies_AtMostOnceTommand_when_tommand_handler_fails : EndpointHostTestBase
{
   protected override async Task InitializeAsyncInternal()
   {
      await base.InitializeAsyncInternal();
      const string exceptionTessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
      MyCreateTaggregateTommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionTessage));

      Host.AssertThatRunningScenarioThrowsBackendAndClientException<TransactionAbortedException>(action: () => ClientEndpoint.ExecuteClientRequest(navigator => navigator.Post(MyCreateTaggregateTommand.Create())));
      await Task.CompletedTask;
   }

   [PCT] public void ExactlyOnce_Tevent_raised_in_handler_does_not_reach_remote_handler()
   {
      MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(count: 1, 1.Seconds())
                                             .Must()
                                             .Be(expected: false);
   }

   [PCT] public void Tommand_handler_is_tried_5_times() => MyCreateTaggregateTommandHandlerThreadGate.Passed.Must().Be(expected: 5);

   [PCT] public void ExactlyOnce_Tevent_raised_in_handler_reaches_local_handler_5_times() => MyLocalTaggregateTeventHandlerThreadGate.Passed.Must().Be(expected: 5);
}
