using Compze.Tessaging.Internals.Transport;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Must;
using Compze.Tessaging.Internals.Transport.Exceptions;
using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>Observation — the transaction-ignoring subscription kind (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>):<br/>
/// a handler registered through <c>ObserveTevents</c> observes committed facts only — queued for it when the publishing unit of<br/>
/// work commits locally, or on arrival remotely (an arriving tevent was committed by its publisher) — dispatched off-thread in<br/>
/// per-observer FIFO order, outside any transaction.</summary>
public class Tevent_observation_tests : EndpointHostTestBase
{
   [PCT] public void Observer_on_the_publishing_endpoint_fires_when_a_tevent_is_published_locally()
   {
      Navigator.Post(MyCreateTaggregateTommand.Create());

      MyTaggregateTeventBackendObserverThreadGate.AwaitPassedThroughCountEqualTo(1);
   }

   [PCT] public void Observer_on_the_publishing_endpoint_does_not_observe_tevents_published_in_a_transaction_that_rolls_back()
   {
      MyCreateTaggregateTommandHandlerThreadGate.FailTransactionOnPreparePostPassThrough(new Exception("50A21F9C-8E0E-4E36-83AF-C2A6DE7B0980"));

      Invoking(() => Navigator.Post(MyCreateTaggregateTommand.Create()))
                    .Must().Throw<MessageDispatchingFailedException>();

      //Deterministic without any waiting: a tevent is queued for its observers only when its publishing unit of work commits,
      //and every server-side retry of the tommand's transaction rolled back - no attempt's publish was ever a committed fact.
      MyTaggregateTeventBackendObserverThreadGate.Passed.Must().Be(0);
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
