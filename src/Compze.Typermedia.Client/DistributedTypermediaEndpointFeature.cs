using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Serialization.Internal;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Typermedia.HandlerRegistration;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

///<summary>
/// Wires the distributed Typermedia pipeline into an endpoint: everything in-process Typermedia has
/// (<see cref="InProcessTypermediaEndpointFeature"/>, which it composes), plus the handler executor serving
/// remote clients, and discovery. Created idempotently through
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
///</summary>
public class DistributedTypermediaEndpointFeature
{
   public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   readonly EndpointTransportServerFeature _transportServer;

   ///<summary>Declares that the endpoint announces where it listens to <paramref name="announcer"/> — see<br/>
   /// <see cref="EndpointTransportServerFeature.AnnounceAddressTo"/>, to which this delegates: the announced address is the<br/>
   /// endpoint's one transport-server address, serving every distributed capability the endpoint speaks.</summary>
   public DistributedTypermediaEndpointFeature AnnounceAddressTo(IEndpointAddressAnnouncer announcer)
   {
      _transportServer.AnnounceAddressTo(announcer);
      return this;
   }

   internal DistributedTypermediaEndpointFeature(IEndpointBuilder builder)
   {
      AssertTheEndpointsFoundationIsDeclared(builder.Registrar);

      builder.TypeMapper.MapTypesFromAssemblyContaining<TypermediaEndpointInformation>(); // Compze.Typermedia.Client — the typermedia discovery types

      RegisterHandlers = builder.AddInProcessTypermedia().RegisterHandlers;
      _transportServer = EndpointTransportServerFeature.GetOrAddTo(builder);

      TypermediaHandlerExecutor.RegisterWith(builder.Registrar);
      builder.Registrar.TypermediaTransportServer();

      builder.OnContainerBuilt(resolver => TypermediaEndpointDiscoveryQueryRegistration.RegisterQueryHandlers(
                                  new EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<EndpointDiscoveryQueryExecutor>())));

      builder.AddComponent(_ => new DistributedTypermediaEndpointComponent(_transportServer));
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
