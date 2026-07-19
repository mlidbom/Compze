namespace Compze.Tessaging.Engine;

///<summary>The builder through which a LocalTessagingEngine — the tessage-conversing heart of one container — is declared, in<br/>
/// one visible block; the declaration is the one and only way anything gets into the engine. Every declaration surface follows<br/>
/// one idiom: a builder method takes an <see cref="Action{T}"/> over a short-lived registrar and returns the builder, so<br/>
/// declarations chain, and the registrar exists only inside its callback — the callback's end is the registration's end, the<br/>
/// build closes the roster, and any attempt to register afterward explodes.</summary>
///<remarks>Type mappings are not declared here: a component declares the assemblies whose type identity it needs where it is<br/>
/// registered, through <c>Registrar.RequireMappedTypesFromAssemblyContaining&lt;T&gt;()</c>. The same declaration block is the<br/>
/// surface everywhere — a plain container (<see cref="LocalTessagingEngineRegistrar.LocalTessagingEngine"/>) or an endpoint —<br/>
/// so an application's handler registrations run unchanged under any composition.</remarks>
public sealed class LocalTessagingEngineBuilder
{
   internal TessageHandlerRegistrations HandlerRegistrations { get; } = new();

   internal LocalTessagingEngineBuilder() {}

   ///<summary>Declares handlers for all four tessage kinds through the one <see cref="TessageHandlerRegistrar"/> — the tessage's<br/>
   /// own type carries its kind, guarantee, and synchrony, so the verbs differ only by handler shape.</summary>
   public LocalTessagingEngineBuilder RegisterTessageHandlers(Action<TessageHandlerRegistrar> register)
   {
      var registrar = new TessageHandlerRegistrar(HandlerRegistrations);
      register(registrar);
      registrar.EndCallback();
      return this;
   }

   ///<summary>Declares tevent observers through the <see cref="TeventObservationRegistrar"/> — observation, the deliberately<br/>
   /// transaction-ignoring watch surface, under its own verb so the distinct semantics are visible at the declaration site.</summary>
   public LocalTessagingEngineBuilder ObserveTevents(Action<TeventObservationRegistrar> observe)
   {
      var registrar = new TeventObservationRegistrar(HandlerRegistrations);
      observe(registrar);
      registrar.EndCallback();
      return this;
   }
}
