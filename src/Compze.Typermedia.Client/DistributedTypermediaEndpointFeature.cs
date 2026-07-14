using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.Internals.Transport;
using Compze.Typermedia.HandlerRegistration;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

///<summary>
/// Wires the distributed Typermedia pipeline into an endpoint: everything in-process Typermedia has
/// (<see cref="InProcessTypermediaEndpointFeature"/>, which it composes), plus the handler executor serving
/// remote clients, and discovery. Created idempotently through
/// <see cref="EndpointBuilderDistributedTypermediaExtensions.AddDistributedTypermedia"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: this is how distributed Typermedia plugs into a
/// hosting mechanism that knows nothing of it, and the feature instance is the handle through which the
/// endpoint's typermedia handlers are registered (<see cref="RegisterHandlers"/>).
///
/// Serving is done by the endpoint's one transport server (<see cref="EndpointTransportServerFeature"/>,
/// which it composes): Typermedia contributes its request handling to that server rather than running a
/// server of its own. The endpoint's address is exposed as the <c>TypermediaAddress</c> extension property
/// (<see cref="EndpointTypermediaExtensions"/>).
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
      builder.TypeMapper.MapTypesFromAssemblyContaining<TypermediaEndpointInformation>(); // Compze.Typermedia.Client — the typermedia discovery types

      RegisterHandlers = builder.AddInProcessTypermedia().RegisterHandlers;
      _transportServer = EndpointTransportServerFeature.GetOrAddTo(builder);

      TypermediaHandlerExecutor.RegisterWith(builder.Registrar);

      builder.OnContainerBuilt(resolver => TypermediaEndpointDiscoveryQueryRegistration.RegisterQueryHandlers(
                                  new EndpointDiscoveryQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<EndpointDiscoveryQueryExecutor>())));

      builder.AddComponent(_ => new DistributedTypermediaEndpointComponent(_transportServer));
   }
}
