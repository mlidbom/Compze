using System.Transactions;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Must;

using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

public class Transient_tevent_delivery_tests : EndpointHostTestBase
{
   [PCT] public void Transient_tevent_published_in_a_transaction_reaches_the_remote_subscribers_handler_on_commit()
   {
      PublishTransientTeventOnTheBackendInATransaction(sequenceNumber: 1);

      MyTransientTeventRemoteHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Transient_tevent_reaches_the_backends_own_subscriber_through_participation()
   {
      PublishTransientTeventOnTheBackendInATransaction(sequenceNumber: 1);

      MyTransientTeventLocalHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Transient_tevent_published_outside_any_transaction_reaches_the_remote_subscribers_handler()
   {
      BackendEndPoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteInIsolatedScope(scope =>
         scope.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyTransientTevent { SequenceNumber = 1 }));

      MyTransientTeventRemoteHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Transient_tevents_reach_the_remote_subscriber_in_the_order_they_were_published()
   {
      1.Through(10).ForEach(PublishTransientTeventOnTheBackendInATransaction);

      MyTransientTeventRemoteHandlerThreadGate.AwaitPassedThroughCountEqualTo(10);
      RemotelyReceivedTransientTevents.Select(it => it.SequenceNumber).SequenceEqual(1.Through(10)).Must().BeTrue();
   }

   [PCT] public void Transient_tevent_published_in_a_transaction_that_rolls_back_never_reaches_the_remote_subscriber()
   {
      Invoking(() => BackendEndPoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
                    {
                       Transaction.Current!.FailOnPrepare();
                       unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(new MyTransientTevent { SequenceNumber = 1 });
                    }))
                   .Must().Throw<TransactionAbortedException>();

      MyTransientTeventRemoteHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(1))
                                              .Must()
                                              .Be(false, "a transient tevent is delivered on commit, so a rolled-back transaction must never leak it");
   }
}
