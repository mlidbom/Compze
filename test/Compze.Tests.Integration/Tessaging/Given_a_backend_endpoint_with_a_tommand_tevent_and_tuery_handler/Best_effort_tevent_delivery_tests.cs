using System.Transactions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Must;
using Compze.Tessaging.Abstractions.TessageBus;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Best_effort_tevent_delivery_tests : EndpointHostTestBase
{
   [PCT] public void Best_effort_tevent_published_in_a_transaction_reaches_the_remote_subscribers_handler_on_commit()
   {
      PublishBestEffortTeventOnTheBackendInATransaction(sequenceNumber: 1);

      MyBestEffortTeventRemoteHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Best_effort_tevent_reaches_the_backends_own_subscriber_through_participation()
   {
      PublishBestEffortTeventOnTheBackendInATransaction(sequenceNumber: 1);

      MyBestEffortTeventLocalHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Best_effort_tevent_published_through_the_independent_publisher_reaches_the_remote_subscribers_handler()
   {
      BackendEndPoint.ServiceLocator.Resolve<IIndependentTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = 1 });

      MyBestEffortTeventRemoteHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Best_effort_tevents_reach_the_remote_subscriber_in_the_order_they_were_published()
   {
      1.Through(10).ForEach(PublishBestEffortTeventOnTheBackendInATransaction);

      MyBestEffortTeventRemoteHandlerThreadGate.AwaitPassedThroughCountEqualTo(10);
      RemotelyReceivedBestEffortTevents.Select(it => it.SequenceNumber).SequenceEqual(1.Through(10)).Must().BeTrue();
   }

   [PCT] public void Best_effort_tevent_published_in_a_transaction_that_rolls_back_never_reaches_the_remote_subscriber()
   {
      Invoking(() => BackendEndPoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
                    {
                       Transaction.Current!.FailOnPrepare();
                       unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyBestEffortTevent { SequenceNumber = 1 });
                    }))
                   .Must().Throw<TransactionAbortedException>();

      MyBestEffortTeventRemoteHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(1))
                                              .Must()
                                              .Be(false, "a best-effort tevent is delivered on commit, so a rolled-back transaction must never leak it");
   }
}
