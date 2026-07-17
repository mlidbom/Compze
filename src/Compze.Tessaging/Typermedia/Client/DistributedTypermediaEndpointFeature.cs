using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Serialization.Internal;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Internals.Transport;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Typermedia.HandlerRegistration;
using Compze.Tessaging.Typermedia.Hosting;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>
/// Wires the distributed Typermedia pipeline into an endpoint: everything in-process Typermedia has
/// (<see cref="InProcessTypermediaEndpointFeature"/>, which it composes), plus the handler executor serving
/// remote clients, and the client side through which the endpoint itself navigates other endpoints'
/// typermedia (<see cref="IRemoteTypermediaNavigator"/>). Created idempotently through
/// <see cref="EndpointBuilderDistributedTypermediaExtensions.AddDistributedTypermedia(IEndpointBuilder)"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: this is how distributed Typermedia plugs into a
/// hosting mechanism that knows nothing of it, and the feature instance is the handle through which the
/// endpoint's typermedia handlers are registered (<see cref="RegisterHandlers"/>).
///
/// The distributed substrate — the one transport server, the one router, discovery, and peer memory — is the
/// distributed Tessaging core's (<see cref="DistributedTessagingEndpointFeature"/>, which this feature
/// composes): typermedia tessages route through the endpoint's one router exactly as every other tessage kind
/// does (<see cref="TypermediaRouting"/>), and the endpoint's one advertisement carries its typermedia types
/// beside its TessageBus ones. The feature registers Typermedia's request-handling contribution
/// (<see cref="TypermediaTransportServerRegistrar.TypermediaTransportServer"/>) itself — protocol-free, so the
/// composing layer declares only the endpoint's transport protocol. The endpoint's address is exposed as the
/// <c>TypermediaAddress</c> extension property (<see cref="EndpointTypermediaExtensions"/>).
///
/// How the endpoint finds the endpoints it navigates and is found by them is declared on the feature itself:
/// <see cref="DiscoverEndpointsThrough"/> (the read side), <see cref="AnnounceAddressTo"/> (the write side), or
/// <see cref="ParticipateIn{TRegistry}"/> (both at once, for a registry that has both faces — a same-machine
/// suite's interprocess registry) — each delegating to the composed Tessaging core, where the one router's
/// topology is declared. Declaring no registry means the endpoint only serves: navigating from it fails loud
/// naming the missing declaration (an external client connects to an explicitly known address instead —
/// e.g. <c>TypermediaTestClient</c>).
///</summary>
public class DistributedTypermediaEndpointFeature
{
   public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   readonly DistributedTessagingEndpointFeature _tessagingCore;

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/> — see<br/>
   /// <see cref="EndpointTransportServerFeature.AnnounceAddressTo"/>, to which this delegates through the composed Tessaging<br/>
   /// core: the announced address is the endpoint's one transport-server address, serving every distributed capability the<br/>
   /// endpoint speaks.</summary>
   public DistributedTypermediaEndpointFeature AnnounceAddressTo(IEndpointAddressAnnouncer announcer)
   {
      _tessagingCore.AnnounceAddressTo(announcer);
      return this;
   }

   ///<summary>Declares the registry through which this endpoint discovers the endpoints whose typermedia it navigates — the read<br/>
   /// side of discovery, whose write side is <see cref="AnnounceAddressTo"/>. Delegates to the composed Tessaging core: the<br/>
   /// endpoint's one router keeps reconciling its connections against the registry's membership, and typermedia routes ride<br/>
   /// those connections. Declaring none means the endpoint only serves — navigating from it fails loud naming this declaration<br/>
   /// as the missing piece.</summary>
   public DistributedTypermediaEndpointFeature DiscoverEndpointsThrough(IEndpointRegistry registry)
   {
      _tessagingCore.DiscoverEndpointsThrough(registry);
      return this;
   }

   ///<summary>Declares that the endpoint participates in <paramref name="registry"/>: it discovers the other endpoints through it<br/>
   /// (<see cref="DiscoverEndpointsThrough"/>) AND announces its own listening address to it (<see cref="AnnounceAddressTo"/>) —<br/>
   /// the composition a same-machine application suite uses, where every process both finds the others and is found by them.<br/>
   /// Declare the two sides separately instead when a deployment is asymmetric.</summary>
   public DistributedTypermediaEndpointFeature ParticipateIn<TRegistry>(TRegistry registry) where TRegistry : IEndpointRegistry, IEndpointAddressAnnouncer
      => DiscoverEndpointsThrough(registry)
        .AnnounceAddressTo(registry);

   internal DistributedTypermediaEndpointFeature(IEndpointBuilder builder)
   {
      AssertTheEndpointsFoundationIsDeclared(builder.Registrar);

      //The distributed substrate is the Tessaging core's: one transport server, one router, one advertisement, peer memory.
      _tessagingCore = builder.AddDistributedTessaging();
      RegisterHandlers = builder.AddInProcessTypermedia().RegisterHandlers;

      TypermediaHandlerExecutor.RegisterWith(builder.Registrar);
      builder.Registrar.TypermediaTransportServer()
             .TypermediaTransport()
             .TypermediaRouting()
             .RemoteTypermediaNavigator();

      //The Typermedia side's share of the endpoint's one advertisement (see IEndpointAdvertisementContributor).
      builder.Registrar.Register(Singleton.ForSet<IEndpointAdvertisementContributor>()
                                          .CreatedBy((ITypermediaHandlerRegistry handlerRegistry) => new TypermediaAdvertisementContributor(handlerRegistry)));
   }

   ///<summary>Distributed Typermedia builds on declarations the feature cannot make itself: the endpoint's transport protocol and<br/>
   /// the Typermedia serializer. Each missing one fails here, when the feature is added, with an error naming the declaration —<br/>
   /// instead of surfacing later as a dependency-resolution failure deep inside the container.</summary>
   static void AssertTheEndpointsFoundationIsDeclared(IComponentRegistrar register)
   {
      State.Assert(register.IsRegistered<IEndpointTransportServer>(),
                   () => "The endpoint declares no transport protocol. Declare it before adding distributed Typermedia — e.g. ComposeEndpoint(it => it.NamedPipeEndpointTransport()...) — or register NamedPipeEndpointTransport()/AspNetCoreEndpointTransport().");
      State.Assert(register.IsRegistered<ITypermediaSerializer>(),
                   () => "The endpoint declares no Typermedia serializer. Fill the serializer slot when adding the feature — e.g. AddDistributedTypermedia(typermedia => typermedia.NewtonsoftSerializer()) — or register one (e.g. NewtonsoftTypermediaSerializer()) before adding it.");
   }
}
