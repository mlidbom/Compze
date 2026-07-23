using System.Collections.Concurrent;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageTypes;
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
/// Two best-effort endpoints (<see cref="BestEffortEndpointDeclaration{TIdentity}"/>) converse in best-effort tevents:
/// guarantee-free Tessaging with no outbox, no inbox, and no database anywhere in either endpoint. Everything exactly-once
/// is exactly what such an endpoint cannot speak: the tier's declaration base has no exactly-once doors, and a declaration
/// that reaches the demand through the surfaces that remain — the general <c>Declare</c> door, the shared observation
/// door — fails loud at composition. The host is the production host and the environment is the specifications' own —
/// nothing is pre-registered, so the composition stands entirely on what it declares.
///</summary>
public class Given_two_best_effort_endpoints : UniversalTestBase
{
   static readonly WaitTimeout HandlerTimeout = WaitTimeout.Seconds(30);

   readonly IEndpointHost _host;
   readonly BestEffortEndpoint _publisherEndpoint;
   readonly IThreadGate _subscriberBestEffortTeventHandlerGate = IThreadGate.NewOpen(HandlerTimeout, "subscriberBestEffortTeventHandler");
   readonly ConcurrentQueue<IMyBestEffortTevent> _bestEffortTeventsHandledOnTheSubscriber = new();

   public Given_two_best_effort_endpoints()
   {
      _host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(),
                                             new CurrentTestsBestEffortEnvironment(new AddressesOfTheHostsEndpoints(() => _host!.Endpoints)));

      _publisherEndpoint = _host.RegisterEndpoint(new PublisherEndpointDeclaration());
      _host.RegisterEndpoint(new SubscriberEndpointDeclaration(this));
   }

   class PublisherEndpointDeclaration : BestEffortEndpointDeclaration<PublisherEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "BestEffortPublisherEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("6D0A3A3E-59C8-4B0B-9E51-2F47A68D31C4"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      ///<summary>The declaration states the relationship: a tevent published before the subscriber's first contact — the two<br/>
      /// endpoints start in parallel and discover each other by reconciliation — is held for it and delivered on the meet,<br/>
      /// instead of being lost to the startup race (queue-before-first-contact).</summary>
      protected override IReadOnlyList<EndpointId> RequiredPeers => [SubscriberEndpointDeclaration.Id];
   }

   class SubscriberEndpointDeclaration : BestEffortEndpointDeclaration<SubscriberEndpointDeclaration>, IEndpointIdentity
   {
      public static string Name => "BestEffortSubscriberEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("A4C1F7D2-8B35-49E0-B6A9-5D21C30E79F8"));

      readonly Given_two_best_effort_endpoints _specification;
      internal SubscriberEndpointDeclaration(Given_two_best_effort_endpoints specification) => _specification = specification;

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) => handle
         .ForTevent((IMyBestEffortTevent tevent) =>
          {
             _specification._bestEffortTeventsHandledOnTheSubscriber.Enqueue(tevent);
             _specification._subscriberBestEffortTeventHandlerGate.AwaitPassThrough();
          });
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
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new CurrentTestsBestEffortEnvironment());
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationDemandingAnExactlyOnceTeventSubscription()))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task registering_a_transaction_ignoring_handler_for_a_tevent_declaring_the_exactly_once_contract_fails_at_composition()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new CurrentTestsBestEffortEnvironment());
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationDemandingExactlyOnceTeventObservation()))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task registering_a_handler_for_a_tommand_declaring_the_exactly_once_contract_fails_at_composition()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new CurrentTestsBestEffortEnvironment());
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationDemandingAnExactlyOnceTommandHandler()))
        .Must().Throw<Exception>().Which.Message.Must().Contain("wires no exactly-once delivery machinery");
   }

   [PCT] public async Task composing_an_endpoint_without_a_serializer_fails_loud_naming_the_missing_declaration()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new EnvironmentDeclaringNoSerializer());
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationWithoutASerializer()))
        .Must().Throw<Exception>().Which.Message.Must().Contain("The endpoint declares no serializer");
   }

   [PCT] public async Task composing_an_endpoint_without_a_transport_protocol_fails_loud_naming_the_missing_declaration()
   {
      await using var host = EndpointHost.Production.Create(() => TestEnv.DIContainer.CreateTestingContainerBuilder(), new EnvironmentDeclaringNoTransportProtocol());
      Invoking(() => host.RegisterEndpoint(new EndpointDeclarationWithoutATransportProtocol()))
        .Must().Throw<Exception>().Which.Message.Must().Contain("The endpoint declares no transport protocol");
   }

   ///<summary>The tier's declaration base makes an exactly-once tevent subscription structurally inexpressible — there is no<br/>
   /// door for it — so this declaration reaches the demand through the one surface where it can still be written, the general<br/>
   /// <c>Declare</c> door, pinning the build-time roster assert as the last line of defense.</summary>
   class EndpointDeclarationDemandingAnExactlyOnceTeventSubscription : BestEffortEndpointDeclaration<EndpointDeclarationDemandingAnExactlyOnceTeventSubscription>, IEndpointIdentity
   {
      public static string Name => "ExactlyOnceSubscriptionOnABestEffortEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("00ABB329-30C5-41BF-BB12-5684C59D6FE9"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void Declare(BestEffortEndpointBuilder endpoint) =>
         endpoint.RegisterTessageBusHandlers(handle => handle.ForTevent((ITeventDeclaringTheExactlyOnceContract _) => Task.CompletedTask));
   }

   ///<summary>The observation door is a shared door on every tier, so this exactly-once demand is expressible at declaration<br/>
   /// time — the build-time roster assert catches it instead.</summary>
   class EndpointDeclarationDemandingExactlyOnceTeventObservation : BestEffortEndpointDeclaration<EndpointDeclarationDemandingExactlyOnceTeventObservation>, IEndpointIdentity
   {
      public static string Name => "ExactlyOnceObservationOnABestEffortEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("7B4B44F9-8873-49A0-B5C7-4E66A01726BF"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void ObserveTevents(ITeventObservationRegistrar observe) => observe.ForTevent((ITeventDeclaringTheExactlyOnceContract _) => {});
   }

   ///<summary>The tier's declaration base makes an exactly-once tommand handler structurally inexpressible — there is no<br/>
   /// door for it — so this declaration reaches the demand through the general <c>Declare</c> door, pinning the build-time<br/>
   /// roster assert as the last line of defense.</summary>
   class EndpointDeclarationDemandingAnExactlyOnceTommandHandler : BestEffortEndpointDeclaration<EndpointDeclarationDemandingAnExactlyOnceTommandHandler>, IEndpointIdentity
   {
      public static string Name => "ExactlyOnceTommandHandlerOnABestEffortEndpoint";
      public static EndpointId Id { get; } = new(Guid.Parse("88459CC0-4099-4BE0-9D23-5BAAECEEDB61"));

      protected override void RegisterComponents(IComponentRegistrar registrar) => registrar.RequireIntegrationTestTypeMappings();

      protected override void Declare(BestEffortEndpointBuilder endpoint) =>
         endpoint.RegisterTessageBusHandlers(handle => handle.ForTommand((TommandDeclaringTheExactlyOnceContract _) => Task.CompletedTask));
   }

   class EndpointDeclarationWithoutASerializer : BestEffortEndpointDeclaration<EndpointDeclarationWithoutASerializer>, IEndpointIdentity
   {
      public static string Name => "BestEffortEndpointWithoutASerializer";
      public static EndpointId Id { get; } = new(Guid.Parse("056B4390-E16B-4E43-941A-632C35EAEAEA"));
   }

   class EndpointDeclarationWithoutATransportProtocol : BestEffortEndpointDeclaration<EndpointDeclarationWithoutATransportProtocol>, IEndpointIdentity
   {
      public static string Name => "BestEffortEndpointWithoutATransportProtocol";
      public static EndpointId Id { get; } = new(Guid.Parse("3C1FD1F9-C52E-455F-97D8-1F409D83F84B"));
   }

   ///<summary>An environment that deliberately declares no serializer — only the transport protocol — so composing in it pins the missing-serializer foundation assert.</summary>
   class EnvironmentDeclaringNoSerializer : IEndpointEnvironment
   {
      public void DeclareOn(EndpointBuilder endpointBuilder) =>
         endpointBuilder.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());

      public void DeclareDomainDatabaseOn(ExactlyOnceEndpointBuilder endpointBuilder) {}
   }

   ///<summary>An environment that deliberately declares no transport protocol — only the serializer — so composing in it pins the missing-transport foundation assert.</summary>
   class EnvironmentDeclaringNoTransportProtocol : IEndpointEnvironment
   {
      public void DeclareOn(EndpointBuilder endpointBuilder) =>
         endpointBuilder.NewtonsoftSerializer();

      public void DeclareDomainDatabaseOn(ExactlyOnceEndpointBuilder endpointBuilder) {}
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

   protected internal class TommandDeclaringTheExactlyOnceContract : Remotable.ExactlyOnce.Tommand;
}
