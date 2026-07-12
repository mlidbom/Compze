using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia;

///<summary>
/// Wires in-process Typermedia into an endpoint: the handler registry and the
/// <see cref="IInProcessTypermediaNavigator"/> through which strictly local tueries and tommands execute
/// synchronously, on the calling thread, within the caller's transaction. Wires no transport server and no
/// discovery, so the endpoint has no Typermedia runtime lifecycle at all. Created idempotently through
/// <see cref="EndpointBuilderTypermediaExtensions.AddInProcessTypermedia"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>, and the feature instance is the handle through
/// which the endpoint's typermedia handlers are registered (<see cref="RegisterHandlers"/>).
///</summary>
///<remarks>
/// Distributed Typermedia contains this feature: <c>AddDistributedTypermedia()</c> (in
/// Compze.Typermedia.Client) composes it and adds the transport server through which remote clients execute
/// the endpoint's remotable handlers, the handler executor serving them, and discovery.
///</remarks>
public class InProcessTypermediaEndpointFeature
{
   public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   internal InProcessTypermediaEndpointFeature(IEndpointBuilder builder)
   {
      var handlerRegistry = new TypermediaHandlerRegistry(builder.TypeMap);
      RegisterHandlers = new TypermediaHandlerRegistrarWithDependencyInjectionSupport(handlerRegistry);

      builder.Registrar.Register(Singleton.For<ITypermediaHandlerRegistry, ITypermediaHandlerRegistrar>().Instance(handlerRegistry))
                       .InProcessTypermediaNavigator();
   }
}
