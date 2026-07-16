using System.Transactions;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Must;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>The publish escape hatch (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>):<br/>
/// <see cref="ITransactionIgnoringTeventPublisher"/> publishes immediately and unconditionally — no on-commit deferral, so the<br/>
/// tevent is emitted even if the caller's transaction rolls back.</summary>
public class Transaction_ignoring_tevent_publisher_tests : EndpointHostTestBase
{
   [PCT] public void Transient_tevent_published_in_a_transaction_that_rolls_back_still_reaches_the_remote_subscribers_handler()
   {
      PublishTransientTeventThroughTheTransactionIgnoringPublisherInATransactionThatRollsBack();

      MyTransientTeventRemoteHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Transient_tevent_published_in_a_transaction_that_rolls_back_still_reaches_the_backends_own_subscriber()
   {
      PublishTransientTeventThroughTheTransactionIgnoringPublisherInATransactionThatRollsBack();

      MyTransientTeventLocalHandlerThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   void PublishTransientTeventThroughTheTransactionIgnoringPublisherInATransactionThatRollsBack() =>
      Invoking(() => BackendEndPoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(scope =>
                    {
                       Transaction.Current!.FailOnPrepare();
                       scope.Resolve<ITransactionIgnoringTeventPublisher>().Publish(new MyTransientTevent { SequenceNumber = 1 });
                    }))
                   .Must().Throw<TransactionAbortedException>();
}
