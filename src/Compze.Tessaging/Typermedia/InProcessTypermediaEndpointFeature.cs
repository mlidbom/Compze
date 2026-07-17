using Compze.Abstractions.Hosting.Public;
using Compze.Tessaging.Engine;

namespace Compze.Tessaging.Typermedia;

///<summary>
/// Wires in-process Typermedia into an endpoint: the endpoint's one engine
/// (<see cref="LocalTessagingEngineFeature"/> — the handler roster and the one executor, shared with every
/// other style feature on the endpoint) and the
/// <see cref="ILocalTypermediaNavigatorSession"/> through which strictly local tueries and tommands execute
/// synchronously, on the calling thread, in the caller's session — a tommand within the caller's transaction.
/// Wires no transport server and no
/// discovery, so the endpoint has no Typermedia runtime lifecycle at all. Created idempotently through
/// <see cref="EndpointBuilderTypermediaExtensions.AddInProcessTypermedia"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>, and the endpoint's typermedia handlers are
/// declared through <see cref="RegisterTessageHandlers"/> — one registrar covers all four tessage kinds,
/// every path declaring into the endpoint's one engine.
///</summary>
///<remarks>
/// Distributed Typermedia contains this feature: <c>AddDistributedTypermedia()</c> (in
/// Compze.Tessaging.Typermedia.Client) composes it and adds the transport server through which remote clients execute
/// the endpoint's remotable handlers, the handler executor serving them, and discovery.
///</remarks>
public class InProcessTypermediaEndpointFeature
{
   readonly LocalTessagingEngineFeature _engine;

   ///<summary>Declares handlers for all four tessage kinds into the endpoint's one engine — see<br/>
   /// <see cref="LocalTessagingEngineBuilder.RegisterTessageHandlers"/>, whose declaration idiom this is.</summary>
   public InProcessTypermediaEndpointFeature RegisterTessageHandlers(Action<TessageHandlerRegistrar> register)
   {
      _engine.RegisterTessageHandlers(register);
      return this;
   }

   internal InProcessTypermediaEndpointFeature(IEndpointBuilder builder)
   {
      _engine = LocalTessagingEngineFeature.GetOrAddTo(builder);

      builder.Registrar.LocalTypermediaNavigatorSession()
                       .IndependentLocalTypermediaNavigator();
   }
}
