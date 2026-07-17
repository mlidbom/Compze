using Compze.Abstractions.Hosting.Public;

namespace Compze.Tessaging.Engine;

///<summary>The endpoint's one engine — roster, executor, and the declaration surface they are built from — created idempotently<br/>
/// (<see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>) so that every style feature composing the endpoint shares the same<br/>
/// engine: one engine per container, whichever feature arrives first brings it. The style features' declaration verbs<br/>
/// (<c>RegisterTessageHandlers</c>/<c>ObserveTevents</c>) all delegate here, into the endpoint's one<br/>
/// <see cref="LocalTessagingEngineBuilder"/>. An interim shim — the concrete endpoint types replace the feature machinery, and<br/>
/// with it this class, when they land.</summary>
class LocalTessagingEngineFeature
{
   internal LocalTessagingEngineBuilder EngineBuilder { get; } = new();

   internal static LocalTessagingEngineFeature GetOrAddTo(IEndpointBuilder builder) => builder.GetOrAddFeature(it => new LocalTessagingEngineFeature(it));

   LocalTessagingEngineFeature(IEndpointBuilder builder) => builder.Registrar.RegisterLocalTessagingEngineCore(EngineBuilder.HandlerRegistrations);

   internal void RegisterTessageHandlers(Action<TessageHandlerRegistrar> register) => EngineBuilder.RegisterTessageHandlers(register);

   internal void ObserveTevents(Action<TeventObservationRegistrar> observe) => EngineBuilder.ObserveTevents(observe);
}
