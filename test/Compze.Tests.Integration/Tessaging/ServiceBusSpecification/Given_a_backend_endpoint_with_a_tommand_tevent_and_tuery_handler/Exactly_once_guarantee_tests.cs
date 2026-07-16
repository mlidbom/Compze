using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using System.Transactions;
using Compze.Tessaging.Hosting;
using Compze.Internals.Transport;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Must;

using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Exactly_once_guarantee_tests : EndpointHostTestBase
{
   [PCT] public void ExactlyOnceTommand_handler_executes_exactly_once()
   {
      RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new MyExactlyOnceTommand());

      MyExactlyOnceTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
      MyExactlyOnceTommandHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(2, WaitTimeout.Seconds(2))
                              .Must()
                              .Be(false, "handler should execute exactly once");
   }

   [PCT] public void ExactlyOnceTevent_remote_handler_executes_exactly_once()
   {
      Navigator.Post(MyCreateTaggregateTommand.Create());

      MyRemoteTaggregateTeventHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
      MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(2, WaitTimeout.Seconds(2))
                                             .Must()
                                             .Be(false, "remote tevent handler should execute exactly once");
   }

   [PCT] public void ExactlyOnceTommand_handler_executes_exactly_once_even_when_handler_is_slow()
   {
      MyExactlyOnceTommandHandlerThreadGate.Close();

      RemoteEndpoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new MyExactlyOnceTommand());

      MyExactlyOnceTommandHandlerThreadGate.AwaitQueueLengthEqualTo(1);

      // Wait long enough for the retry poller to have fired multiple times (polling at 500ms, first backoff at 500ms)
      Thread.Sleep(2.Seconds());

      MyExactlyOnceTommandHandlerThreadGate.Open();
      MyExactlyOnceTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
      MyExactlyOnceTommandHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(2, WaitTimeout.Seconds(2))
                              .Must()
                              .Be(false, "handler should execute exactly once even when slow");
   }

   [PCT] public void If_transaction_fails_after_successfully_Sending_ExactlyOnceTommand_tommand_never_reaches_tommand_handler()
   {
      Invoking(() => RemoteEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
                    {
                       Transaction.Current!.FailOnPrepare();
                       unitOfWork.Resolve<IUnitOfWorkTommandSender>().Send(new MyExactlyOnceTommand());
                    }))
                   .Must().Throw<TransactionAbortedException>();

      MyExactlyOnceTommandHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(1))
                              .Must()
                              .Be(false, "tommand should not reach handler");
   }

   [PCT] public void If_transaction_fails_after_successfully_Publishing_ExactlyOnceTevent_tevent_never_reaches_remote_handler_but_does_reach_local_handler()
   {
      const string exceptionTessage = "82369B6E-80D4-4E64-92B6-A564A7195CC5";
      MyCreateTaggregateTommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception(exceptionTessage));

      var frontEndException = Invoking(() => Navigator.Post(MyCreateTaggregateTommand.Create()))
                                    .Must().Throw<MessageDispatchingFailedException>().Which;

      frontEndException.Message.Must().Contain(exceptionTessage);

      MyLocalTaggregateTeventHandlerThreadGate.Passed.Must().BeGreaterThanOrEqualTo(1);

      MyRemoteTaggregateTeventHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(1))
                                             .Must()
                                             .Be(false, "tevent should not reach handler");
   }
}
