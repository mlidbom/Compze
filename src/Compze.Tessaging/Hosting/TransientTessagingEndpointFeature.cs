using Compze.Abstractions.Configuration.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TransientDelivery;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tessaging.Transport;
using Compze.Internals.Transport.NamedPipes;

namespace Compze.Tessaging.Hosting;

///<summary>
/// Wires guarantee-free distributed Tessaging into an endpoint — the transport-speaking Tessaging core, which
/// the full exactly-once pipeline (<see cref="ExactlyOnceTessagingEndpointFeature"/>) composes and extends:
/// everything in-process Tessaging has (<see cref="InProcessTessagingEndpointFeature"/>, which it composes),
/// plus the endpoint's one transport server, the router that connects to the other endpoints, and the
/// transient tevent delivery leg. An endpoint with only this feature converses in transient tevents — a
/// published <see cref="Compze.Abstractions.Tessaging.Public.IRemotableTevent"/> crosses the wire best-effort,
/// with no outbox, no inbox, and no database anywhere (see <c>dev_docs/tevent-delivery-model.md</c>) — so it
/// composes on the database-less <see cref="EndpointFoundation"/>. Everything exactly-once is exactly what it
/// cannot speak: registering a handler for a tessage type that demands the exactly-once contract fails at
/// setup, and publishing an exactly-once tevent fails naming the missing delivery leg. Created idempotently
/// through <see cref="EndpointBuilderTessagingExtensions.AddTransientTessaging"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>, and the feature instance is the handle through
/// which the endpoint's tessaging handlers are registered (<see cref="RegisterHandlers"/>).
///
/// Serving is done by the endpoint's one transport server (<see cref="EndpointTransportServerFeature"/>,
/// which it composes): the feature registers the transient tier's request-handling contribution
/// (<see cref="TransientTessagingRequestHandlersRegistrar.TransientTessagingRequestHandlers"/>) and the client
/// that posts tessages (<see cref="TransportMessagePosterRegistrar.TessagingTransportMessagePoster"/>) itself —
/// both protocol-free, so the composing layer declares only the endpoint's transport protocol
/// (e.g. <see cref="NamedPipeEndpointTransportRegistrar.NamedPipeEndpointTransport(IComponentRegistrar)"/>).
///
/// How the endpoint finds other endpoints and is found by them is declared on the feature itself:
/// <see cref="DiscoverEndpointsThrough"/> (the read side), <see cref="AnnounceAddressTo"/> (the write side), or
/// <see cref="ParticipateIn{TRegistry}"/> (both at once, for a registry that has both faces — a same-machine
/// suite's interprocess registry). One registration is guarded with <c>IsRegistered</c> so a hosting layer can
/// pre-register its own before the feature is added: the in-flight tracker (a testing host supplies a real one
/// to await quiescence; the default does nothing). The runtime lifecycle lives in
/// <see cref="TransientTessagingEndpointComponent"/>, and the endpoint's address is exposed as the
/// <c>TessagingAddress</c> extension property (<see cref="EndpointTessagingExtensions"/>).
///</summary>
public class TransientTessagingEndpointFeature
{
   readonly ITessageHandlerRegistrar _handlerRegistrar;
   readonly ITransactionIgnoringTeventHandlerRegistrar _transactionIgnoringTeventHandlerRegistrar;

   public TransientTessagingEndpointFeature RegisterHandlers(Action<ITessageHandlerRegistrar> registrar)
   {
      registrar(_handlerRegistrar);
      return this;
   }

   ///<summary>Registers transaction-ignoring tevent handlers — observation, the one subscription-side opt-down<br/>
   /// (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>): the handler fires once, immediately, when a matching<br/>
   /// tevent is published locally or arrives from another endpoint, outside any transaction and with no delivery guarantees.</summary>
   public TransientTessagingEndpointFeature RegisterTransactionIgnoringTeventHandlers(Action<ITransactionIgnoringTeventHandlerRegistrar> registrar)
   {
      registrar(_transactionIgnoringTeventHandlerRegistrar);
      return this;
   }

   readonly EndpointTransportServerFeature _transportServer;
   IEndpointRegistry? _endpointRegistry;

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/> — see<br/>
   /// <see cref="EndpointTransportServerFeature.AnnounceAddressTo"/>, to which this delegates: the announced address is the<br/>
   /// endpoint's one transport-server address, serving every distributed capability the endpoint speaks.</summary>
   public TransientTessagingEndpointFeature AnnounceAddressTo(IEndpointAddressAnnouncer announcer)
   {
      _transportServer.AnnounceAddressTo(announcer);
      return this;
   }

   ///<summary>Declares the registry through which this endpoint discovers the endpoints it converses with — the read side of discovery,<br/>
   /// whose write side is <see cref="AnnounceAddressTo"/>. The endpoint's router keeps reconciling its connections against the<br/>
   /// registry's membership. Declaring none means other endpoints' addresses come from application configuration<br/>
   /// (<see cref="AppConfigEndpointRegistry"/>).</summary>
   public TransientTessagingEndpointFeature DiscoverEndpointsThrough(IEndpointRegistry registry)
   {
      State.Assert(_endpointRegistry is null, () => $"The endpoint already declared the registry it discovers endpoints through — an endpoint discovers through exactly one {nameof(IEndpointRegistry)}.");
      _endpointRegistry = registry;
      return this;
   }

   ///<summary>Declares that the endpoint participates in <paramref name="registry"/>: it discovers the other endpoints through it<br/>
   /// (<see cref="DiscoverEndpointsThrough"/>) AND announces its own listening address to it (<see cref="AnnounceAddressTo"/>) —<br/>
   /// the composition a same-machine application suite uses, where every process both finds the others and is found by them.<br/>
   /// Declare the two sides separately instead when a deployment is asymmetric.</summary>
   public TransientTessagingEndpointFeature ParticipateIn<TRegistry>(TRegistry registry) where TRegistry : IEndpointRegistry, IEndpointAddressAnnouncer
      => DiscoverEndpointsThrough(registry)
        .AnnounceAddressTo(registry);

   internal TransientTessagingEndpointFeature(IEndpointBuilder builder)
   {
      var register = builder.Registrar;
      AssertTheEndpointsFoundationIsDeclared(register);

      builder.TypeMapper.MapTypesFromAssemblyContaining<TessagingEndpointInformationQuery>(); // Compze.Tessaging — the tessaging discovery types

      var inProcessTessaging = builder.AddInProcessTessaging();
      _handlerRegistrar = inProcessTessaging.RegisterHandlers;
      _transactionIgnoringTeventHandlerRegistrar = inProcessTessaging.RegisterTransactionIgnoringTeventHandlers;
      _transportServer = EndpointTransportServerFeature.GetOrAddTo(builder);

      if(!register.IsRegistered<ITessagesInFlightTracker>())
      {
         register.Register(Singleton.For<ITessagesInFlightTracker>().CreatedBy(() => new NullOpTessagesInFlightTracker()));
      }

      //The background-exception reporter arrives with the in-process core the feature composes above.
      register.TaskRunner()
              .TessagingTransport()
              .TransientTeventDelivery()
              .TessagingTransportMessagePoster()
              .TransientTessagingRequestHandlers();

      builder.OnContainerBuilt(resolver =>
      {
         var handlerRegistry = resolver.Resolve<ITessageHandlerRegistry>();
         AssertNoRegisteredHandlerDemandsMoreThanTheEndpointDelivers(register, handlerRegistry);
         //Advertisement soundness fails at endpoint setup, not when the first peer queries: it asserts that every advertised type has a TypeId mapping and gets a route on the peers' routers.
         handlerRegistry.HandledRemoteTessageTypeIds();

         TessagingEndpointDiscoveryQueryRegistration.RegisterQueryHandlers(
            new EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<EndpointDiscoveryQueryExecutor>()));
      });

      builder.AddComponent(resolver => new TransientTessagingEndpointComponent(resolver, _transportServer, EndpointRegistry(resolver)));
   }

   ///<summary>The registry the endpoint declared through <see cref="DiscoverEndpointsThrough"/>, or — when it declared none — the<br/>
   /// fallback that reads other endpoints' addresses from application configuration.</summary>
   IEndpointRegistry EndpointRegistry(IRootResolver resolver) =>
      _endpointRegistry ??= new AppConfigEndpointRegistry(resolver.Resolve<IConfigurationParameterProvider>());

   ///<summary>The setup-time wiring rule (see <c>dev_docs/tevent-delivery-model.md</c>): a subscription demanding more than the<br/>
   /// endpoint can deliver fails at setup. On an endpoint whose composition wires no exactly-once machinery — no<br/>
   /// <see cref="IInbox"/> to persist, dedup, and retry — a registered handler for a tessage type that declares the exactly-once<br/>
   /// contract could never be honored: advertising it would pull exactly-once traffic the endpoint must refuse, stalling every<br/>
   /// sender's in-order delivery to it. Silently downgrading the guarantee instead is data loss dressed as success.</summary>
   static void AssertNoRegisteredHandlerDemandsMoreThanTheEndpointDelivers(IComponentRegistrar register, ITessageHandlerRegistry handlerRegistry)
   {
      if(register.IsRegistered<IInbox>()) return; //The exactly-once machinery is wired: every declarable delivery contract is deliverable.

      var typesDemandingExactlyOnceDelivery = handlerRegistry.RegisteredTypesDemandingExactlyOnceDelivery();
      State.Assert(typesDemandingExactlyOnceDelivery.Count == 0,
                   () => $"This endpoint wires no exactly-once delivery machinery — transient Tessaging has no inbox to persist, dedup, and retry with — but handlers are registered for tessage types whose declared contract demands it: {string.Join(", ", typesDemandingExactlyOnceDelivery.Select(it => it.FullName))}. A subscription takes the tessage type's full declared guarantee (observation included: observing a remote exactly-once tevent still requires receiving it exactly-once), and an endpoint that cannot honor a guarantee must not advertise for it. Compose exactly-once Tessaging on a database-backed foundation instead, or handle tessage types that declare no exactly-once contract.");
   }

   ///<summary>Transient Tessaging builds on declarations the feature cannot make itself: the endpoint's transport protocol and the<br/>
   /// Tessaging serializer — deliberately no persistence: the guarantee-free composition is the one that needs no database. Each<br/>
   /// missing declaration fails here, when the feature is added, with an error naming it — instead of surfacing later as a<br/>
   /// dependency-resolution failure deep inside the container.</summary>
   static void AssertTheEndpointsFoundationIsDeclared(IComponentRegistrar register)
   {
      State.Assert(register.IsRegistered<IEndpointTransportServer>(),
                   () => "The endpoint declares no transport protocol. Declare it before adding transient Tessaging — e.g. ComposeEndpoint(it => it.NamedPipeEndpointTransport()...) — or register NamedPipeEndpointTransport()/AspNetCoreEndpointTransport().");
      State.Assert(register.IsRegistered<ITessagingSerializer>(),
                   () => "The endpoint declares no Tessaging serializer. Fill the serializer slot when adding the feature — e.g. AddTransientTessaging(tessaging => tessaging.NewtonsoftSerializer()) — or register one (e.g. NewtonsoftTessagingSerializer()) before adding it.");
   }
}
