using Compze.Abstractions.Serialization.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Tessaging.TessageHandling.Registration.Public;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.Peers;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Transport.SqlLayer;
using Compze.Tessaging.Transport;

namespace Compze.Tessaging.Hosting;

///<summary>
/// Wires the full exactly-once Tessaging pipeline — inbox, outbox, and the tommand senders
/// (<see cref="Compze.Abstractions.Tessaging.Public.IUnitOfWorkTommandSender"/> /
/// <see cref="Compze.Abstractions.Tessaging.Public.IIndependentTommandSender"/>) —
/// into an endpoint: everything the transport-speaking distributed core has
/// (<see cref="DistributedTessagingEndpointFeature"/>, which it composes — the transport server, the router,
/// the peer registry, and the best-effort tevent delivery leg — itself composing
/// <see cref="InProcessTessagingEndpointFeature"/>), plus the exactly-once vertical. The peer registry the
/// composed core registers is durable here (<see cref="Compze.Tessaging.Implementation.Peers.IPeerRegistry"/>):
/// this feature's foundation declares Tessaging persistence, so peer memory lives in the endpoint's database
/// and survives restarts. Wiring the outbox is what wires the endpoint's durable tevent delivery leg,
/// through which the endpoint's <see cref="Compze.Abstractions.Tessaging.Public.IUnitOfWorkTeventPublisher"/> routes
/// every published <see cref="Compze.Abstractions.Tessaging.Public.IExactlyOnceTevent"/> to its remote
/// subscribers — and what grants each of the router's connections its durable, restart-surviving exactly-once
/// delivery stream. Created idempotently through
/// <see cref="EndpointBuilderTessagingExtensions.AddExactlyOnceTessaging"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: this is how exactly-once Tessaging plugs into a
/// hosting mechanism that knows nothing of it, and the feature instance is the handle through which the
/// endpoint's tessaging handlers are registered (<see cref="RegisterHandlers"/>).
///
/// The feature registers the exactly-once request handling served by the endpoint's one transport server
/// (<see cref="ExactlyOnceTessagingRequestHandlersRegistrar.ExactlyOnceTessagingRequestHandlers"/>) — arriving
/// exactly-once tevents and tommands are received into the inbox. Discovery — how the endpoint finds other
/// endpoints and is found by them — is the composed core's concern, reached through the delegating
/// <see cref="DiscoverEndpointsThrough"/>, <see cref="AnnounceAddressTo"/>, and <see cref="ParticipateIn{TRegistry}"/>.
/// The exactly-once pipeline's runtime lifecycle lives in <see cref="ExactlyOnceTessagingEndpointComponent"/>.
///</summary>
public class ExactlyOnceTessagingEndpointFeature
{
   readonly DistributedTessagingEndpointFeature _distributedTessagingCore;

   public ExactlyOnceTessagingEndpointFeature RegisterHandlers(Action<ITessageHandlerRegistrar> registrar)
   {
      _distributedTessagingCore.RegisterHandlers(registrar);
      return this;
   }

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/> — see<br/>
   /// <see cref="EndpointTransportServerFeature.AnnounceAddressTo"/>, to which this delegates: the announced address is the<br/>
   /// endpoint's one transport-server address, serving every distributed capability the endpoint speaks.</summary>
   public ExactlyOnceTessagingEndpointFeature AnnounceAddressTo(IEndpointAddressAnnouncer announcer)
   {
      _distributedTessagingCore.AnnounceAddressTo(announcer);
      return this;
   }

   ///<summary>Declares the registry through which this endpoint discovers the endpoints it converses with — the read side of discovery,<br/>
   /// whose write side is <see cref="AnnounceAddressTo"/>. The endpoint's router keeps reconciling its connections against the<br/>
   /// registry's membership. Declaring none means the endpoint discovers nothing: it serves whatever reaches it, converses<br/>
   /// in-process, and self-sends (its router maintains the connection to its own inbox, which needs no discovery) — but it<br/>
   /// connects to no other endpoint.</summary>
   public ExactlyOnceTessagingEndpointFeature DiscoverEndpointsThrough(IEndpointRegistry registry)
   {
      _distributedTessagingCore.DiscoverEndpointsThrough(registry);
      return this;
   }

   ///<summary>Declares that the endpoint participates in <paramref name="registry"/>: it discovers the other endpoints through it<br/>
   /// (<see cref="DiscoverEndpointsThrough"/>) AND announces its own listening address to it (<see cref="AnnounceAddressTo"/>) —<br/>
   /// the composition a same-machine application suite uses, where every process both finds the others and is found by them.<br/>
   /// Declare the two sides separately instead when a deployment is asymmetric.</summary>
   public ExactlyOnceTessagingEndpointFeature ParticipateIn<TRegistry>(TRegistry registry) where TRegistry : IEndpointRegistry, IEndpointAddressAnnouncer
      => DiscoverEndpointsThrough(registry)
        .AnnounceAddressTo(registry);

   internal ExactlyOnceTessagingEndpointFeature(IEndpointBuilder builder)
   {
      var register = builder.Registrar;
      AssertTheEndpointsFoundationIsDeclared(register);

      _distributedTessagingCore = builder.AddDistributedTessaging();

      register.Outbox()
              .Inbox()
              .UnitOfWorkTommandSender()
              .IndependentTommandSender()
              .ExactlyOnceTessagingRequestHandlers();

      builder.AddComponent(resolver => new ExactlyOnceTessagingEndpointComponent(resolver));
   }

   ///<summary>Exactly-once Tessaging builds on declarations the feature cannot make itself: the endpoint's transport protocol, its<br/>
   /// persistence, and the Tessaging serializer. Each missing one fails here, when the feature is added, with an error naming the<br/>
   /// declaration — instead of surfacing later as a dependency-resolution failure deep inside the container.</summary>
   static void AssertTheEndpointsFoundationIsDeclared(IComponentRegistrar register)
   {
      State.Assert(register.IsRegistered<IEndpointTransportServer>(),
                   () => "The endpoint declares no transport protocol. Declare it before adding exactly-once Tessaging — e.g. ComposeEndpoint(it => it.NamedPipeEndpointTransport()...) — or register NamedPipeEndpointTransport()/AspNetCoreEndpointTransport().");
      State.Assert(register.IsRegistered<ITessagingSqlLayer.IInboxSqlLayer>() && register.IsRegistered<ITessagingSqlLayer.IOutboxSqlLayer>() && register.IsRegistered<ITessagingSqlLayer.IPeerRegistrySqlLayer>(),
                   () => "The endpoint declares no Tessaging persistence. Add the feature on a foundation whose database is declared — e.g. ComposeEndpoint(it => ...SqliteEndpointDatabase(...)).AddExactlyOnceTessaging(...) — or register Tessaging's sql layers (e.g. SqliteTessagingSqlLayer()) before adding it. An endpoint that deliberately persists nothing speaks guarantee-free Tessaging instead: AddDistributedTessaging(...).");
      State.Assert(register.IsRegistered<ITessagingSerializer>(),
                   () => "The endpoint declares no Tessaging serializer. Fill the serializer slot when adding the feature — e.g. AddExactlyOnceTessaging(tessaging => tessaging.NewtonsoftSerializer()) — or register one (e.g. NewtonsoftTessagingSerializer()) before adding it.");
   }
}
