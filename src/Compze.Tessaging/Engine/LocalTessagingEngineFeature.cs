using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Engine;

///<summary>The endpoint's one engine core — roster, executor, and the handler registrations they are built from — created<br/>
/// idempotently (<see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>) so that every style feature composing the endpoint<br/>
/// shares the same roster: one engine per container, whichever feature arrives first brings it. An interim shim — the concrete<br/>
/// endpoint types replace the feature machinery, and with it this class, when they land.</summary>
class LocalTessagingEngineFeature
{
   internal TessageHandlerRegistrations HandlerRegistrations { get; }

   internal static LocalTessagingEngineFeature GetOrAddTo(IEndpointBuilder builder) => builder.GetOrAddFeature(it => new LocalTessagingEngineFeature(it));

   LocalTessagingEngineFeature(IEndpointBuilder builder) => HandlerRegistrations = builder.Registrar.RegisterLocalTessagingEngineCore();
}
