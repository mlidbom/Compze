using System.Collections.Concurrent;
using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;

using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Engine;
using Compze.Tests.Common.Tessaging.Given_a_backend_endpoint_with_a_tommand_tevent_and_tuery_handler;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.Hosting;

///<summary>
/// Two best-effort endpoints (<see cref="BestEffortEndpoint.Build"/>) converse in best-effort tevents: guarantee-free
/// Tessaging with no outbox, no inbox, and no database anywhere in either endpoint. Everything exactly-once is exactly what
/// such an endpoint cannot speak: registering a handler for a tessage type declaring the exactly-once contract — either
/// subscription kind — fails loud at composition, and publishing an exactly-once tevent fails loud naming the missing
/// delivery leg. The host is the production host — nothing is pre-registered, so the composition stands entirely on what it
/// declares.
///</summary>
public class Given_two_best_effort_endpoints : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);
   static readonly EndpointId SubscriberEndpointId = new(Guid.Parse("a4c1f7d2-8b35-49e0-b6a9-5d21c30e79f8"));

   readonly IEndpointHost _host;
   readonly BestEffortEndpoint _publisherEndpoint;
   readonly IThreadGate _subscriberBestEffortTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "subscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<IMyBestEffortTevent> _bestEffortTeventsHandledOnTheSubscriber = new();

   public Given_two_best_effort_endpoints()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      var endpointsOfTheHost = new AddressesOfTheHostsEndpoints(() => _host.Endpoints);

      _publisherEndpoint = _host.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "BestEffortPublisherEndpoint",
         new EndpointId(Guid.Parse("6d0a3a3e-59c8-4b0b-9e51-2f47a68d31c4")),
         endpointBuilder => endpointBuilder
            .MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings())
            .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
            .NewtonsoftSerializer()
            .DiscoverEndpointsThrough(endpointsOfTheHost)
            //The composition declares the relationship: a tevent published before the subscriber's first contact - the two
            //endpoints start in parallel and discover each other by reconciliation - is held for it and delivered on the meet,
            //instead of being lost to the startup race (queue-before-first-contact).
            .RequirePeers(SubscriberEndpointId)));

      _host.RegisterEndpoint(container => BestEffortEndpoint.Build(
         container,
         "BestEffortSubscriberEndpoint",
         SubscriberEndpointId,
         endpointBuilder =>
         {
            endpointBuilder
               .MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings())
               .TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport())
               .NewtonsoftSerializer()
               .DiscoverEndpointsThrough(endpointsOfTheHost)
               .RegisterTessageHandlers(handle => handle.ForTevent((IMyBestEffortTevent tevent) =>
                {
                   _bestEffortTeventsHandledOnTheSubscriber.Enqueue(tevent);
                   _subscriberBestEffortTeventHandlerGate.AwaitPassThrough();
                }));
         }));
   }

   protected override async Task InitializeAsyncInternal() => await _host.StartAsync().caf();

   protected override async Task DisposeAsyncInternal() => await _host.DisposeAsync().caf();

   [PCT] public void a_best_effort_tevent_published_on_one_endpoint_is_handled_on_the_other()
   {
      PublishOnThePublisherEndpointInATransaction(new MyBestEffortTevent { SequenceNumber = 1 });

      _subscriberBestEffortTeventHandlerGate.AwaitPassedThroughCountEqualTo(1);
      _bestEffortTeventsHandledOnTheSubscriber.Single().SequenceNumber.Must().Be(1);
   }

   //Published through the async door: an exactly-once tevent's own type contract demands it, and this pin is about the
   //missing delivery leg - the sync door would refuse the tevent one assert earlier, for the synchrony reason.
   [PCT] public async Task publishing_a_tevent_declaring_the_exactly_once_contract_fails_loud_naming_the_missing_delivery_leg() =>
      (await InvokingAsync(async () => await PublishAsyncOnThePublisherEndpointInATransaction(new TeventDeclaringTheExactlyOnceContract()))
        .Must().ThrowAsync<Exception>()).Which.Message.Must().Contain("demands the exactly-once delivery leg");

   [PCT] public void publishing_a_tevent_declaring_the_exactly_once_contract_through_the_synchronous_door_fails_loud_naming_the_async_contract() =>
      Invoking(() => PublishOnThePublisherEndpointInATransaction(new TeventDeclaringTheExactlyOnceContract()))
        .Must().Throw<Exception>().Which.Message.Must().Contain("exactly-once kinds are async end to end");

   [PCT] public async Task registering_a_handler_for_a_tevent_declaring_the_exactly_once_contract_fails_at_composition()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint(container => BestEffortEndpoint.Build(
                        container, "ExactlyOnceSubscriptionOnABestEffortEndpoint", new EndpointId(Guid.NewGuid()),
                        endpointBuilder =>
                        {
                           ComposeMinimalFoundation(endpointBuilder);
                           endpointBuilder.RegisterTessageHandlers(handle => handle.ForTevent((ITeventDeclaringTheExactlyOnceContract _) => Task.CompletedTask));
                        })))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task registering_a_transaction_ignoring_handler_for_a_tevent_declaring_the_exactly_once_contract_fails_at_composition()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint(container => BestEffortEndpoint.Build(
                        container, "ExactlyOnceObservationOnABestEffortEndpoint", new EndpointId(Guid.NewGuid()),
                        endpointBuilder =>
                        {
                           ComposeMinimalFoundation(endpointBuilder);
                           endpointBuilder.ObserveTevents(observe => observe.ForTevent((ITeventDeclaringTheExactlyOnceContract _) => {}));
                        })))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task registering_a_handler_for_a_tommand_declaring_the_exactly_once_contract_fails_at_composition()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint(container => BestEffortEndpoint.Build(
                        container, "ExactlyOnceTommandHandlerOnABestEffortEndpoint", new EndpointId(Guid.NewGuid()),
                        endpointBuilder =>
                        {
                           ComposeMinimalFoundation(endpointBuilder);
                           endpointBuilder.RegisterTessageHandlers(handle => handle.ForTommand((TommandDeclaringTheExactlyOnceContract _) => Task.CompletedTask));
                        })))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task composing_an_endpoint_without_a_serializer_fails_loud_naming_the_missing_declaration()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint(container => BestEffortEndpoint.Build(
                        container, "BestEffortEndpointWithoutASerializer", new EndpointId(Guid.NewGuid()),
                        endpointBuilder => endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport()))))
        .Must().Throw<Exception>().Which.Message.Must().Contain("The endpoint declares no serializer");
   }

   [PCT] public async Task composing_an_endpoint_without_a_transport_protocol_fails_loud_naming_the_missing_declaration()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder());
      Invoking(() => host.RegisterEndpoint(container => BestEffortEndpoint.Build(
                        container, "BestEffortEndpointWithoutATransportProtocol", new EndpointId(Guid.NewGuid()),
                        endpointBuilder => endpointBuilder.NewtonsoftSerializer())))
        .Must().Throw<Exception>().Which.Message.Must().Contain("The endpoint declares no transport protocol");
   }

   static void ComposeMinimalFoundation(BestEffortEndpointBuilder endpointBuilder)
   {
      endpointBuilder.MapTypes(mapper => mapper.RegisterIntegrationTestTypeMappings());
      endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
      endpointBuilder.NewtonsoftSerializer();
   }

   void PublishOnThePublisherEndpointInATransaction(ITevent tevent) =>
      _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWork(unitOfWork =>
                                                                                                      unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().Publish(tevent));

   async Task PublishAsyncOnThePublisherEndpointInATransaction(ITevent tevent) =>
      await _publisherEndpoint.ServiceLocator.Resolve<IScopeFactory>().ExecuteUnitOfWorkAsync(async unitOfWork =>
                                                                                                 await unitOfWork.Resolve<IUnitOfWorkTeventPublisher>().PublishAsync(tevent));

   ///<summary>Knows the address of every endpoint in the host, so each endpoint's router connects to all of them — the<br/>
   /// discovery a production suite gets from a shared registry, with nothing persisted anywhere.</summary>
   class AddressesOfTheHostsEndpoints(Func<IReadOnlyList<IEndpoint>> hostEndpoints) : IEndpointRegistry
   {
      readonly Func<IReadOnlyList<IEndpoint>> _hostEndpoints = hostEndpoints;

      public IEnumerable<EndpointAddress> ServerEndpointAddresses =>
      [
         .. _hostEndpoints().OfType<Endpoint>()
                            .Where(it => it.Address is not null)
                            .Select(it => it.Address!)
      ];
   }

   protected internal interface ITeventDeclaringTheExactlyOnceContract : IExactlyOnceTevent;

   protected internal class TeventDeclaringTheExactlyOnceContract : ITeventDeclaringTheExactlyOnceContract
   {
      public TessageId Id { get; } = new();
   }

   protected internal class TommandDeclaringTheExactlyOnceContract : TessageTypes.Remotable.ExactlyOnce.Tommand;
}
