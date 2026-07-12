using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.Internals.Transport;
using Compze.Typermedia.HandlerRegistration;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

///<summary>
/// Wires the distributed Typermedia pipeline into an endpoint: everything in-process Typermedia has
/// (<see cref="InProcessTypermediaEndpointFeature"/>, which it composes), plus the transport server through
/// which remote clients execute the endpoint's remotable handlers, the handler executor serving them, and
/// discovery. Created idempotently through
/// <see cref="EndpointBuilderDistributedTypermediaExtensions.AddDistributedTypermedia"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: this is how distributed Typermedia plugs into a
/// hosting mechanism that knows nothing of it, and the feature instance is the handle through which the
/// endpoint's typermedia handlers are registered (<see cref="RegisterHandlers"/>).
///
/// The runtime lifecycle lives in <see cref="DistributedTypermediaEndpointComponent"/> (the transport server
/// listens; nothing sends), and the server's address is exposed as the
/// <c>TypermediaAddress</c> extension property (<see cref="EndpointTypermediaExtensions"/>).
///</summary>
public class DistributedTypermediaEndpointFeature
{
   public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   internal DistributedTypermediaEndpointFeature(IEndpointBuilder builder)
   {
      builder.TypeMapper.MapTypesFromAssemblyContaining<TypermediaEndpointInformation>(); // Compze.Typermedia.Client — the typermedia discovery types

      RegisterHandlers = builder.AddInProcessTypermedia().RegisterHandlers;

      TypermediaHandlerExecutor.RegisterWith(builder.Registrar);

      builder.OnContainerBuilt(resolver => TypermediaInfrastructureQueryRegistration.RegisterQueryHandlers(
                                  new InfrastructureQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<InfrastructureQueryExecutor>())));

      builder.AddComponent(resolver => new DistributedTypermediaEndpointComponent(resolver.Resolve<ITypermediaTransportServer>()));
   }
}
