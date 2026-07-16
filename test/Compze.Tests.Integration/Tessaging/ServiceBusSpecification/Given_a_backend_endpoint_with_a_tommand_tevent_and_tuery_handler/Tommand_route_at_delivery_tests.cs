using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Tests.Common.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;

using static Compze.Must.MustActions;

namespace Compze.Tests.Integration.Tessaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;

///<summary>A tommand routes at delivery time, not send time (route-at-delivery — see <c>dev_docs/TODO/durable-peer-topology.md</c>):<br/>
/// it names no recipient — it means "the handler of its type, whoever that is" — so the outbox persists it unbound and it binds<br/>
/// to whichever endpoint advertises its type when delivery happens. Sending validates only that some known route serves the<br/>
/// type: a remembered peer's advertisement, or the endpoint's own handlers.</summary>
[LongRunning]
public class Tommand_route_at_delivery_tests : EndpointHostTestBase
{
   [PCT] public async Task A_tommand_sent_while_the_remembered_handler_endpoint_is_down_is_delivered_when_the_handler_returns()
   {
      //The Backend met the Remote endpoint - the handler of the tommand's type - when the host started; rebuild the host without it: the handler is now down.
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      //The send succeeds inside the caller's unit of work: the Remote endpoint's remembered advertisement is the route that serves the type.
      BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());

      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest(); //The tommand is undelivered: awaiting rest would wait for the handler's return.
      await StartHostAsync();

      MyExactlyOnceTommandHandledByTheRemoteEndpointHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   [PCT] public async Task A_tommand_sent_while_the_handler_endpoint_is_down_is_delivered_to_a_successor_that_advertises_the_tommands_type()
   {
      await Host.DisposeAsync();
      await StartHostWithOnlyTheBackendEndpointAsync();

      BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new MyExactlyOnceTommandHandledByTheRemoteEndpoint());

      //The Remote endpoint never returns; a successor - a NEW endpoint identity advertising the same tommand type - arrives instead: blue/green replacement.
      await Host.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
      await StartHostWithTheBackendEndpointAndASuccessorToTheRemoteEndpointAsync();

      RemoteSuccessorTommandHandlerThreadGate.AwaitPassedThroughCountEqualTo(1, WaitTimeout.Seconds(15));
   }

   [PCT] public void Sending_a_tommand_whose_type_neither_a_remembered_peer_nor_the_endpoint_itself_handles_fails_loudly_naming_the_type()
   {
      var message = Invoking(() => BackendEndPoint.ServiceLocator.Resolve<IIndependentTommandSender>().Send(new MyUnhandledExactlyOnceTommand()))
                   .Must().Throw<Exception>().Which.Message;

      message.Must().Contain(nameof(MyUnhandledExactlyOnceTommand));
      message.Must().Contain("neither this endpoint's own handlers nor any remembered peer's advertisement");
   }
}
