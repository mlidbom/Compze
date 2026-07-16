using Compze.Internals.Transport;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;

using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>Observation — the transaction-ignoring subscription kind (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>):<br/>
/// a handler registered through <c>RegisterTransactionIgnoringTeventHandlers</c> fires once, immediately, when the tevent is<br/>
/// registered — at publish time locally, on arrival remotely — outside any transaction and undeterred by its fate.</summary>
public class Tevent_observation_tests : EndpointHostTestBase
{
   [PCT] public void Observer_on_the_publishing_endpoint_fires_when_a_tevent_is_published_locally()
   {
      Navigator.Post(MyCreateTaggregateTommand.Create());

      MyTaggregateTeventBackendObserverThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Observer_on_the_publishing_endpoint_fires_even_though_the_publishing_transaction_rolls_back()
   {
      MyCreateTaggregateTommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception("50A21F9C-8E0E-4E36-83AF-C2A6DE7B0980"));

      Invoking(() => Navigator.Post(MyCreateTaggregateTommand.Create()))
                    .Must().Throw<MessageDispatchingFailedException>();

      //At least once: the tommand's transaction is retried server-side before the post fails, and every doomed attempt's publish is observed.
      MyTaggregateTeventBackendObserverThreadGate.Passed.Must().BeGreaterThanOrEqualTo(1);
   }

   [PCT] public void Observer_on_a_remote_endpoint_fires_when_the_exactly_once_tevent_arrives_even_while_its_transactional_handler_has_not_executed()
   {
      MyRemoteTaggregateTeventHandlerThreadGate.Close();

      Navigator.Post(MyCreateTaggregateTommand.Create());

      MyTaggregateTeventRemoteObserverThreadGate.AwaitPassedThroughCountEqualTo(1);
      MyRemoteTaggregateTeventHandlerThreadGate.Passed.Must().Be(0);
   }

   [PCT] public void Observer_on_a_remote_endpoint_fires_when_a_best_effort_tevent_arrives()
   {
      PublishBestEffortTeventOnTheBackendInATransaction(sequenceNumber: 1);

      MyBestEffortTeventRemoteObserverThreadGate.AwaitPassedThroughCountEqualTo(1);
   }
}
