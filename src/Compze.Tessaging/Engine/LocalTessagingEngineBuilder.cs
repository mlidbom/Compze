using Compze.Tessaging.Engine.HandlerRegistration._private;
using Compze.Tessaging.Engine.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Typermedia;

namespace Compze.Tessaging.Engine;

///<summary>The builder through which a LocalTessagingEngine — the tessage-conversing heart of one container — is declared, in<br/>
/// one visible block; the declaration is the one and only way anything gets into the engine. Every registration surface follows<br/>
/// one idiom: a builder method takes an <see cref="Action{T}"/> over a short-lived registrar and returns the builder, so<br/>
/// declarations chain, and the registrar exists only inside its callback — the callback's end is the registration's end, the<br/>
/// build closes the roster, and any attempt to register afterward explodes. The one exception is observation:<br/>
/// <see cref="ITeventObservation"/> creates dynamic observers, which come and go while the engine runs, kept apart from the<br/>
/// roster, which stays closed.</summary>
///<remarks>Type mappings are not declared here: a component declares the assemblies whose type identity it needs where it is<br/>
/// registered, through <c>Registrar.RequireMappedTypesFromAssemblyContaining&lt;T&gt;()</c>. The same declaration block is the<br/>
/// surface everywhere — a plain container (<see cref="LocalTessagingEngineRegistrar.LocalTessagingEngine"/>) or an endpoint —<br/>
/// so an application's handler registrations run unchanged under any composition.</remarks>
public sealed class LocalTessagingEngineBuilder
{
   internal TessageHandlerRegistrations HandlerRegistrations { get; }

   ///<summary>The builder of a plain container's engine, or, if <paramref name="isTheEngineOfAnEndpoint"/>, of an endpoint's.</summary>
   internal LocalTessagingEngineBuilder(bool isTheEngineOfAnEndpoint = false) => HandlerRegistrations = new TessageHandlerRegistrations(isTheEngineOfAnEndpoint);

   ///<summary>Declares handlers for the TessageBus kinds — tevents and tommands whose type declares no result — through the<br/>
   /// <see cref="TessageBusHandlerRegistrar"/>; the tessage's own type carries its guarantee, locality, and synchrony.</summary>
   public LocalTessagingEngineBuilder RegisterTessageBusHandlers(Action<TessageBusHandlerRegistrar> register)
   {
      var registrar = new TessageBusHandlerRegistrar(HandlerRegistrations);
      register(registrar);
      registrar.EndCallback();
      return this;
   }

   ///<summary>Declares handlers for the Typermedia kinds — the conversational tessages whose handler answers a caller: tommands<br/>
   /// whose type declares a result, and tueries — through the <see cref="TypermediaHandlerRegistrar"/>.</summary>
   public LocalTessagingEngineBuilder RegisterTypermediaHandlers(Action<TypermediaHandlerRegistrar> register)
   {
      var registrar = new TypermediaHandlerRegistrar(HandlerRegistrations);
      register(registrar);
      registrar.EndCallback();
      return this;
   }

   ///<summary>Declares composed tevent observers, which observe for as long as the container lives, through the<br/>
   /// <see cref="TeventObservationRegistrar"/> — observation, the deliberately transaction-ignoring watch surface, under its own<br/>
   /// verb so the distinct semantics are visible at the declaration site. Dynamic observers, which come and go while the engine<br/>
   /// runs, are created through <see cref="ITeventObservation"/>.</summary>
   public LocalTessagingEngineBuilder ObserveTevents(Action<TeventObservationRegistrar> observe)
   {
      var registrar = new TeventObservationRegistrar(HandlerRegistrations.AddTeventObserver);
      observe(registrar);
      registrar.EndCallback();
      return this;
   }
}
