using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tessaging.Abstractions;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>A tommand binds to its one specific receiver at send time — the live handler when one is connected, otherwise the<br/>
/// sole remembered peer whose advertisement handles the type — so a handler being down never makes the send explode, while<br/>
/// every tessage between a sender and a receiver rides that pair's single ordered, receiver-deduped delivery stream: the<br/>
/// exactly-once in-order guarantee holds by construction (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>, which also records<br/>
/// why routing at delivery time was tried and retracted).</summary>
[LongRunning]
public class Tommand_receiver_binding_tests : EndpointHostTestBase
{
   [PCT] public async Task A_tommand_sent_while_the_remembered_handler_endpoint_is_down_is_delivered_when_the_handler_returns()
   {
      //The Backend met the Remote endpoint - the handler of the tommand's type - when the host started; rebuild the host without it: the handler is now down.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      //The send succeeds inside the caller's unit of work, bound to the remembered Remote endpoint.
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //The tommand is undelivered: awaiting rest would wait for the handler's return.
      await StartHostAsync();

      MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   [PCT] public async Task A_tommand_bound_to_its_known_receiver_waits_for_that_endpoint_and_never_delivers_to_a_successor_while_new_sends_follow_the_live_successor()
   {
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      //Bound to the remembered Remote endpoint - the pair's stream is where its exactly-once in-order guarantee lives.
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());

      //A successor - a NEW endpoint identity advertising the same tommand type - arrives while the Remote endpoint is still down.
      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
      await StartHostWithTheBackendEndpointAndASuccessorToTheRemoteEndpointAsync();

      //The bound tommand must not enter another endpoint's stream: the successor never receives it...
      RemoteSuccessorTommandHandlerThreadGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(2)).Must().BeFalse();

      //...while a NEW tommand binds to the live handler - the successor - and is delivered to it...
      await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());
      RemoteSuccessorTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));

      //...and the waiting tommand still reaches its own endpoint when it returns.
      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
      await StartHostAsync();
      MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   [PCT] public async Task Sending_a_tommand_while_multiple_remembered_peers_advertise_its_type_and_none_is_live_fails_after_patience_naming_the_type()
   {
      //The Backend met the Remote endpoint at the initial start; meeting a successor too leaves TWO remembered peers advertising the type.
      await Host.DisposeAsync();
      await StartHostWithTheBackendEndpointAndASuccessorToTheRemoteEndpointAsync();

      //With neither of them live there is no way to know which is current: binding to the wrong one would strand the tommand.
      //The send waits out the Backend's handler-availability patience for one of them to connect, then fails loud.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      var message = (await InvokingAsync(async () => await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyExactlyOnceTommandHandledByTheRemoteEndpoint()))
                   .Must().ThrowAsync<Exception>()).Which.Message;

      message.Must().Contain(nameof(MyExactlyOnceTommandHandledByTheRemoteEndpoint));
      message.Must().Contain("More than one remembered peer");
      message.Must().Contain("handler-availability patience");
   }

   [PCT] public async Task Sending_a_tommand_whose_type_neither_a_remembered_peer_nor_the_endpoint_itself_handles_fails_after_patience_naming_the_type()
   {
      //The send waits out the Backend's handler-availability patience for a first contact with an endpoint handling the type, then fails loud.
      var message = (await InvokingAsync(async () => await BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().SendAsync(new MyUnhandledExactlyOnceTommand()))
                   .Must().ThrowAsync<Exception>()).Which.Message;

      message.Must().Contain(nameof(MyUnhandledExactlyOnceTommand));
      message.Must().Contain("neither this endpoint's own handlers nor any remembered peer's advertisement");
      message.Must().Contain("handler-availability patience");
   }
}
