using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Hosting;

///<summary>
/// Wires in-process Tessaging into an endpoint — the style's synchronous core, which distribution composes
/// and extends: the handler registry, the synchronous in-process tevent delivery every tevent travels
/// (<see cref="IInProcessTeventPublisher"/>), and the endpoint's one <see cref="IUnitOfWorkTeventPublisher"/>, which
/// routes each published tevent by the delivery contract its type declares. With nothing but this feature the
/// endpoint wires no remote delivery legs, so tevents are delivered synchronously, on the publishing thread,
/// within the publisher's transaction, to this process's handlers. Created idempotently through
/// <see cref="EndpointBuilderTessagingExtensions.AddInProcessTessaging"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>, and the feature instance is the handle through
/// which the endpoint's tessaging handlers are registered (<see cref="RegisterHandlers"/>).
///</summary>
///<remarks>
/// Whether a tevent crosses the wire is not an endpoint-wide mode but a property of each tevent's type,
/// honored by the delivery legs the composition wires (<c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>) —
/// <see cref="DistributedTessagingEndpointFeature"/> composes this feature and wires the best-effort leg, and
/// <see cref="ExactlyOnceTessagingEndpointFeature"/> composes that core and wires the durable leg. That is
/// what keeps handler registration (<see cref="RegisterHandlers"/>) order-independent of every other
/// Tessaging declaration.
///</remarks>
public class InProcessTessagingEndpointFeature
{
   public ITessageHandlerRegistrar RegisterHandlers { get; }

   ///<summary>Registers transaction-ignoring tevent handlers — observation, the subscription-side escape hatch: the handler fires<br/>
   /// once, immediately, when the tevent is published locally or arrives from another endpoint, outside any transaction and with no<br/>
   /// delivery guarantees (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>).</summary>
   public ITransactionIgnoringTeventHandlerRegistrar RegisterTransactionIgnoringTeventHandlers { get; }

   internal InProcessTessagingEndpointFeature(IEndpointBuilder builder)
   {
      builder.TypeMapper.MapTypesFromAssemblyContaining<ITaggregateTevent>(); // Compze.Core — the Teventive type hierarchy

      var handlerRegistry = new TessageHandlerRegistry(builder.TypeMap);
      RegisterHandlers = handlerRegistry;
      RegisterTransactionIgnoringTeventHandlers = handlerRegistry;

      builder.Registrar.Register(Singleton.For<ITessageHandlerRegistry, ITessageHandlerRegistrar, ITransactionIgnoringTeventHandlerRegistrar>().Instance(handlerRegistry))
                       .BackgroundExceptionReporter()
                       .InProcessTeventPublisher()
                       .TeventObservationDispatcher()
                       .UnitOfWorkTeventPublisher()
                       .IndependentTeventPublisher();
   }
}
