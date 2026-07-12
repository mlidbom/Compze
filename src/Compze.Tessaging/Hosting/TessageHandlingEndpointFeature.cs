using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Teventive.TeventStore.Internal;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Hosting;

///<summary>
/// Wires tessage handling into an endpoint: the handler registry and the synchronous in-process tevent
/// delivery (<see cref="IInProcessTeventPublisher"/>) — the leg every tevent travels, whatever the endpoint
/// speaks. Created idempotently through
/// <see cref="EndpointBuilderTessagingExtensions.AddTessageHandling"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>, and the feature instance is the handle through
/// which the endpoint's tessaging handlers are registered (<see cref="RegisterHandlers"/>).
///</summary>
///<remarks>
/// Deliberately declares no tevent publication mode: it registers no
/// <see cref="ITeventStoreTeventPublisher"/>. An endpoint whose taggregates publish through a tevent store
/// declares its mode explicitly — <see cref="EndpointBuilderTessagingExtensions.AddInProcessTessaging"/> or
/// <see cref="EndpointBuilderTessagingExtensions.AddDistributedTessaging"/> — which keeps handler
/// registration order-independent of that declaration.
///</remarks>
public class TessageHandlingEndpointFeature
{
   public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   internal TessageHandlingEndpointFeature(IEndpointBuilder builder)
   {
      builder.TypeMapper.MapTypesFromAssemblyContaining<ITaggregateTevent>(); // Compze.Core — the Teventive type hierarchy

      var handlerRegistry = new TessageHandlerRegistry(builder.TypeMap);
      RegisterHandlers = new TessageHandlerRegistrarWithDependencyInjectionSupport(handlerRegistry);

      builder.Registrar.Register(Singleton.For<ITessageHandlerRegistry, ITessageHandlerRegistrar>().Instance(handlerRegistry))
                       .InProcessTeventPublisher();
   }
}
