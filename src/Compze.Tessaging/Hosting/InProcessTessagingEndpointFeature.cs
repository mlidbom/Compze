using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Implementation;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Hosting;

///<summary>
/// Wires in-process Tessaging into an endpoint — the style's synchronous core, which distribution composes
/// and extends: the endpoint's one engine (<see cref="LocalTessagingEngineFeature"/> — the handler roster and
/// the one executor, shared with every other style feature on the endpoint) and the endpoint's one
/// <see cref="IUnitOfWorkTeventPublisher"/>, which routes each published tevent by the delivery contract its
/// type declares. With nothing but this feature the endpoint wires no remote delivery legs, so tevents are
/// delivered synchronously, on the publishing thread, within the publisher's transaction, to this process's
/// handlers. Created idempotently through
/// <see cref="EndpointBuilderTessagingExtensions.AddInProcessTessaging"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>, and the endpoint's tessage handlers are declared
/// through <see cref="RegisterTessageHandlers"/> — on this feature, on any feature composing it, or on the
/// endpoint builder itself (<see cref="EndpointBuilderTessagingExtensions.RegisterTessageHandlers"/>): every
/// path declares into the endpoint's one engine.
///</summary>
///<remarks>
/// Whether a tevent crosses the wire is not an endpoint-wide mode but a property of each tevent's type,
/// honored by the delivery legs the composition wires (<c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>) —
/// <see cref="DistributedTessagingEndpointFeature"/> composes this feature and wires the best-effort leg, and
/// <see cref="ExactlyOnceTessagingEndpointFeature"/> composes that core and wires the durable leg. That is
/// what keeps handler declaration order-independent of every other Tessaging declaration.
///</remarks>
public class InProcessTessagingEndpointFeature
{
   readonly LocalTessagingEngineFeature _engine;

   ///<summary>Declares handlers for all four tessage kinds into the endpoint's one engine — see<br/>
   /// <see cref="LocalTessagingEngineBuilder.RegisterTessageHandlers"/>, whose declaration idiom this is.</summary>
   public InProcessTessagingEndpointFeature RegisterTessageHandlers(Action<TessageHandlerRegistrar> register)
   {
      _engine.RegisterTessageHandlers(register);
      return this;
   }

   ///<summary>Declares tevent observers — observation, the deliberately transaction-ignoring watch surface: an observer fires<br/>
   /// once, immediately, when the tevent is published locally or arrives from another endpoint, outside any transaction and with no<br/>
   /// delivery guarantees (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>).</summary>
   public InProcessTessagingEndpointFeature ObserveTevents(Action<TeventObservationRegistrar> observe)
   {
      _engine.ObserveTevents(observe);
      return this;
   }

   internal InProcessTessagingEndpointFeature(IEndpointBuilder builder)
   {
      builder.TypeMapper.MapTypesFromAssemblyContaining<ITaggregateTevent>(); // Compze.Teventive — the Teventive type hierarchy

      _engine = LocalTessagingEngineFeature.GetOrAddTo(builder);

      builder.Registrar.UnitOfWorkTeventPublisher()
                       .IndependentTeventPublisher();
   }
}
