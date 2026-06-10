using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Typermedia.HandlerRegistration;
using Compze.Typermedia.Hosting;

namespace Compze.Typermedia.Client;

///<summary>Wires the Typermedia pipeline — handler registry and executor, in-process navigator, transport server, discovery — into an endpoint.</summary>
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
