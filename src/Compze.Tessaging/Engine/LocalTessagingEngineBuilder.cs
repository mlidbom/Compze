using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Engine;

///<summary>The builder through which a LocalTessagingEngine — the tessage-conversing heart of one container — is declared, in<br/>
/// one visible block; the declaration is the one and only way anything gets into the engine. Every declaration surface follows<br/>
/// one idiom: a builder method takes an <see cref="Action{T}"/> over a short-lived registrar and returns the builder, so<br/>
/// declarations chain, and the registrar exists only inside its callback — the callback's end is the registration's end, the<br/>
/// build closes the roster, and any attempt to register afterward explodes.</summary>
///<remarks>Type mappings are declared on the same builder (<see cref="MapTypes"/>) — they serve persistent stores and, when an<br/>
/// endpoint wraps the engine, the wire. The same declaration block is the surface everywhere — a plain container<br/>
/// (<see cref="LocalTessagingEngineRegistrar.LocalTessagingEngine"/>) or an endpoint — so an application's handler<br/>
/// registrations run unchanged under any composition.</remarks>
public sealed class LocalTessagingEngineBuilder
{
   internal TessageHandlerRegistrations HandlerRegistrations { get; } = new();
   internal List<Action<ITypeMapper>> TypeMappingDeclarations { get; } = [];

   internal LocalTessagingEngineBuilder() {}

   ///<summary>Declares the engine's type-id mappings — the stable identities its tessage types carry into persistent stores and,<br/>
   /// when an endpoint wraps the engine, onto the wire. A strictly-local composition that persists nothing needs none.</summary>
   public LocalTessagingEngineBuilder MapTypes(Action<ITypeMapper> map)
   {
      TypeMappingDeclarations.Add(map);
      return this;
   }

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
