using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Serialization.Internal;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Tessaging.Typermedia.HandlerRegistration;
using Compze.Tessaging.Typermedia.Hosting;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>
/// Wires the distributed Typermedia pipeline into an endpoint: everything in-process Typermedia has
/// (<see cref="InProcessTypermediaEndpointFeature"/>, which it composes), plus the handler executor serving
/// remote clients, discovery, and the client side through which the endpoint itself navigates other
/// endpoints' typermedia (<see cref="IRemoteTypermediaNavigator"/>, routed by the endpoint's
/// <see cref="ITypermediaRouter"/>). Created idempotently through
/// <see cref="EndpointBuilderDistributedTypermediaExtensions.AddDistributedTypermedia(IEndpointBuilder)"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: this is how distributed Typermedia plugs into a
/// hosting mechanism that knows nothing of it, and the feature instance is the handle through which the
/// endpoint's typermedia handlers are registered (<see cref="RegisterHandlers"/>).
///
/// Serving is done by the endpoint's one transport server (<see cref="EndpointTransportServerFeature"/>,
/// which it composes): the feature registers Typermedia's request-handling contribution
/// (<see cref="TypermediaTransportServerRegistrar.TypermediaTransportServer"/>) itself — protocol-free, so the
/// composing layer declares only the endpoint's transport protocol. The endpoint's address is exposed as the
/// <c>TypermediaAddress</c> extension property (<see cref="EndpointTypermediaExtensions"/>).
///
/// How the endpoint finds the endpoints it navigates and is found by them is declared on the feature itself:
/// <see cref="DiscoverEndpointsThrough"/> (the read side), <see cref="AnnounceAddressTo"/> (the write side), or
/// <see cref="ParticipateIn{TRegistry}"/> (both at once, for a registry that has both faces — a same-machine
/// suite's interprocess registry). Declaring no registry means the endpoint only serves: navigating from it
/// fails loud naming the missing declaration (an external client connects to an explicitly known address
/// instead — e.g. <c>TypermediaTestClient</c>). The runtime lifecycle lives in
/// <see cref="DistributedTypermediaEndpointComponent"/>.
///</summary>
public class DistributedTypermediaEndpointFeature
{
   public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   readonly EndpointTransportServerFeature _transportServer;
   IEndpointRegistry? _endpointRegistry;

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/> — see<br/>
   /// <see cref="EndpointTransportServerFeature.AnnounceAddressTo"/>, to which this delegates: the announced address is the<br/>
   /// endpoint's one transport-server address, serving every distributed capability the endpoint speaks.</summary>
   public DistributedTypermediaEndpointFeature AnnounceAddressTo(IEndpointAddressAnnouncer announcer)
   {
      _transportServer.AnnounceAddressTo(announcer);
      return this;
   }

   ///<summary>Declares the registry through which this endpoint discovers the endpoints whose typermedia it navigates — the read<br/>
   /// side of discovery, whose write side is <see cref="AnnounceAddressTo"/>. The endpoint's typermedia router keeps reconciling<br/>
   /// its connections against the registry's membership. Declaring none means the endpoint only serves — navigating from it fails<br/>
   /// loud naming this declaration as the missing piece.</summary>
   public DistributedTypermediaEndpointFeature DiscoverEndpointsThrough(IEndpointRegistry registry)
   {
      State.Assert(_endpointRegistry is null, () => $"The endpoint already declared the registry it discovers endpoints through — an endpoint discovers through exactly one {nameof(IEndpointRegistry)}.");
      _endpointRegistry = registry;
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

      builder.TypeMapper.MapTypesFromAssemblyContaining<TypermediaEndpointInformation>(); // Compze.Tessaging.Typermedia.Client — the typermedia discovery types

      RegisterHandlers = builder.AddInProcessTypermedia().RegisterHandlers;
      _transportServer = EndpointTransportServerFeature.GetOrAddTo(builder);

      TypermediaHandlerExecutor.RegisterWith(builder.Registrar);
      builder.Registrar.TypermediaTransportServer()
             .TypermediaTransport()
             .TypermediaRouter()
             .RemoteTypermediaNavigator();

      builder.OnContainerBuilt(resolver => TypermediaEndpointDiscoveryQueryRegistration.RegisterQueryHandlers(
                                  new EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<EndpointDiscoveryQueryExecutor>())));

      builder.AddComponent(resolver => new DistributedTypermediaEndpointComponent(resolver.Resolve<ITypermediaRouter>(), _transportServer, _endpointRegistry));
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
