using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Typermedia.HandlerRegistration;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

///<summary>
/// Wires the Typermedia pipeline — handler registry and executor, in-process navigator, transport server,
/// discovery — into an endpoint. Created idempotently through
/// <see cref="EndpointBuilderTypermediaExtensions.AddTypermedia"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: this is how a paradigm plugs into the
/// paradigm-blind hosting mechanism, and the feature instance is the handle through which the endpoint's
/// typermedia handlers are registered (<see cref="RegisterHandlers"/>).
///
/// The runtime lifecycle lives in <see cref="TypermediaEndpointComponent"/> (the transport server listens;
/// nothing sends), and the server's address is exposed as the
/// <c>TypermediaAddress</c> extension property (<see cref="EndpointTypermediaExtensions"/>).
///</summary>
public class TypermediaEndpointFeature
{
   public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   internal TypermediaEndpointFeature(IEndpointBuilder builder)
   {
      builder.TypeMapper.MapTypesFromAssemblyContaining<TypermediaEndpointInformation>(); // Compze.Typermedia.Client — the typermedia discovery types

      var handlerRegistry = new TypermediaHandlerRegistry(builder.TypeMap);
      RegisterHandlers = new TypermediaHandlerRegistrarWithDependencyInjectionSupport(handlerRegistry);

      builder.Registrar.Register(Singleton.For<ITypermediaHandlerRegistry, ITypermediaHandlerRegistrar>().Instance(handlerRegistry));
      TypermediaHandlerExecutor.RegisterWith(builder.Registrar);
      builder.Registrar.InProcessTypermediaNavigator();

      builder.OnContainerBuilt(resolver => TypermediaInfrastructureQueryRegistration.RegisterQueryHandlers(
                                  new InfrastructureQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<InfrastructureQueryExecutor>())));

      builder.AddComponent(resolver => new TypermediaEndpointComponent(resolver.Resolve<ITypermediaTransportServer>()));
   }
}
