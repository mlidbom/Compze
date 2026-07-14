using Compze.Abstractions.Configuration.Internal;
using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.Tessaging.Transport;
using Compze.Internals.Transport.NamedPipes;

namespace Compze.Tessaging.Hosting;

///<summary>
/// Wires the distributed Tessaging pipeline — inbox, outbox, tommand scheduler, router, service bus
/// session — into an endpoint: everything in-process Tessaging has
/// (<see cref="InProcessTessagingEndpointFeature"/>, which it composes), plus the machinery through which the
/// endpoint converses with other endpoints. Wiring the outbox is what wires the endpoint's durable tevent
/// delivery leg, through which the endpoint's <see cref="Compze.Abstractions.Tessaging.Public.ITeventPublisher"/>
/// routes every published <see cref="Compze.Abstractions.Tessaging.Public.IExactlyOnceTevent"/> to its remote
/// subscribers. Created idempotently through
/// <see cref="EndpointBuilderTessagingExtensions.AddDistributedTessaging"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: this is how distributed Tessaging plugs into a
/// hosting mechanism that knows nothing of it, and the feature instance is the handle through which the
/// endpoint's tessaging handlers are registered (<see cref="_handlerRegistrar"/>).
///
/// Serving is done by the endpoint's one transport server (<see cref="EndpointTransportServerFeature"/>,
/// which it composes): the feature registers Tessaging's request-handling contribution
/// (<see cref="TessagingTransportServerRegistrar.TessagingTransportServer"/>) and the client that posts tessages
/// (<see cref="TransportMessagePosterRegistrar.TessagingTransportMessagePoster"/>) itself — both protocol-free, so the
/// composing layer declares only the endpoint's transport protocol
/// (e.g. <see cref="NamedPipeEndpointTransportRegistrar.NamedPipeEndpointTransport(IComponentRegistrar)"/>).
///
/// How the endpoint finds other endpoints and is found by them is declared on the feature itself:
/// <see cref="DiscoverEndpointsThrough"/> (the read side), <see cref="AnnounceAddressTo"/> (the write side), or
/// <see cref="ParticipateIn{TRegistry}"/> (both at once, for a registry that has both faces — a same-machine
/// suite's interprocess registry). One registration is guarded with <c>IsRegistered</c> so a hosting layer can
/// pre-register its own before the feature is added: the in-flight tracker (a testing host supplies a real one
/// to await quiescence; the default does nothing). The runtime lifecycle lives in
/// <see cref="DistributedTessagingEndpointComponent"/>, and the endpoint's address is exposed as the
/// <c>TessagingAddress</c> extension property (<see cref="EndpointTessagingExtensions"/>).
///</summary>
public class DistributedTessagingEndpointFeature
{
   readonly TessageHandlerRegistrarWithDependencyInjectionSupport _handlerRegistrar;

   public DistributedTessagingEndpointFeature RegisterHandlers(Action<TessageHandlerRegistrarWithDependencyInjectionSupport> registrar)
   {
      registrar(_handlerRegistrar);
      return this;
   }

   readonly EndpointTransportServerFeature _transportServer;
   IEndpointRegistry? _endpointRegistry;

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/> — see<br/>
   /// <see cref="EndpointTransportServerFeature.AnnounceAddressTo"/>, to which this delegates: the announced address is the<br/>
   /// endpoint's one transport-server address, serving every distributed capability the endpoint speaks.</summary>
   public DistributedTessagingEndpointFeature AnnounceAddressTo(IEndpointAddressAnnouncer announcer)
   {
      _transportServer.AnnounceAddressTo(announcer);
      return this;
   }

   ///<summary>Declares the registry through which this endpoint discovers the endpoints it converses with — the read side of discovery,<br/>
   /// whose write side is <see cref="AnnounceAddressTo"/>. The endpoint's router keeps reconciling its connections against the<br/>
   /// registry's membership. Declaring none means other endpoints' addresses come from application configuration<br/>
   /// (<see cref="AppConfigEndpointRegistry"/>).</summary>
   public DistributedTessagingEndpointFeature DiscoverEndpointsThrough(IEndpointRegistry registry)
   {
      State.Assert(_endpointRegistry is null, () => $"The endpoint already declared the registry it discovers endpoints through — an endpoint discovers through exactly one {nameof(IEndpointRegistry)}.");
      _endpointRegistry = registry;
      return this;
   }

   ///<summary>Declares that the endpoint participates in <paramref name="registry"/>: it discovers the other endpoints through it<br/>
   /// (<see cref="DiscoverEndpointsThrough"/>) AND announces its own listening address to it (<see cref="AnnounceAddressTo"/>) —<br/>
   /// the composition a same-machine application suite uses, where every process both finds the others and is found by them.<br/>
   /// Declare the two sides separately instead when a deployment is asymmetric.</summary>
   public DistributedTessagingEndpointFeature ParticipateIn<TRegistry>(TRegistry registry) where TRegistry : IEndpointRegistry, IEndpointAddressAnnouncer
      => DiscoverEndpointsThrough(registry)
        .AnnounceAddressTo(registry);

   internal DistributedTessagingEndpointFeature(IEndpointBuilder builder)
   {
      var register = builder.Registrar;
      AssertTheEndpointsFoundationIsDeclared(register);

      _handlerRegistrar = builder.AddInProcessTessaging().RegisterHandlers;
      _transportServer = EndpointTransportServerFeature.GetOrAddTo(builder);

      if(!register.IsRegistered<ITessagesInFlightTracker>())
      {
         register.Register(Singleton.For<ITessagesInFlightTracker>().CreatedBy(() => new NullOpTessagesInFlightTracker()));
      }

      register.BackgroundExceptionReporter()
              .TaskRunner()
              .TessagingTransport()
              .TessagingTransportMessagePoster()
              .TessagingTransportServer()
              .Outbox()
              .Inbox()
              .TommandScheduler()
              .ServiceBusSession();

      builder.OnContainerBuilt(resolver => TessageTypesInternal.RegisterEndpointDiscoveryQueryHandlers(
                                  new EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<EndpointDiscoveryQueryExecutor>()),
                                  EndpointRegistry(resolver)));

      builder.AddComponent(resolver => new DistributedTessagingEndpointComponent(resolver, _transportServer, EndpointRegistry(resolver)));
   }

   ///<summary>The registry the endpoint declared through <see cref="DiscoverEndpointsThrough"/>, or — when it declared none — the<br/>
   /// fallback that reads other endpoints' addresses from application configuration.</summary>
   IEndpointRegistry EndpointRegistry(IRootResolver resolver) =>
      _endpointRegistry ??= new AppConfigEndpointRegistry(resolver.Resolve<IConfigurationParameterProvider>());

   ///<summary>Distributed Tessaging builds on declarations the feature cannot make itself: the endpoint's transport protocol, its<br/>
   /// persistence, and the Tessaging serializer. Each missing one fails here, when the feature is added, with an error naming the<br/>
   /// declaration — instead of surfacing later as a dependency-resolution failure deep inside the container.</summary>
   static void AssertTheEndpointsFoundationIsDeclared(IComponentRegistrar register)
   {
      State.Assert(register.IsRegistered<IEndpointTransportServer>(),
                   () => "The endpoint declares no transport protocol. Declare it before adding distributed Tessaging — e.g. ComposeEndpoint(it => it.NamedPipeEndpointTransport()...) — or register NamedPipeEndpointTransport()/AspNetCoreEndpointTransport().");
      State.Assert(register.IsRegistered<IServiceBusSqlLayer.IInboxSqlLayer>() && register.IsRegistered<IServiceBusSqlLayer.IOutboxSqlLayer>(),
                   () => "The endpoint declares no Tessaging persistence. Add the feature on a foundation whose database is declared — e.g. ComposeEndpoint(it => ...SqliteEndpointDatabase(...)).AddDistributedTessaging(...) — or register Tessaging's sql layers (e.g. SqliteTessagingSqlLayer()) before adding it.");
      State.Assert(register.IsRegistered<ITessagingSerializer>(),
                   () => "The endpoint declares no Tessaging serializer. Fill the serializer slot when adding the feature — e.g. AddDistributedTessaging(tessaging => tessaging.NewtonsoftSerializer()) — or register one (e.g. NewtonsoftTessagingSerializer()) before adding it.");
   }
}
