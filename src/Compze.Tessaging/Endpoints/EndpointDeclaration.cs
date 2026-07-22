using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints.BestEffort;
using Compze.Tessaging.Endpoints.ExactlyOnce;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Typermedia;

namespace Compze.Tessaging.Endpoints;

///<summary>
/// The base of the two endpoint-declaration tiers (<see cref="ExactlyOnceEndpointDeclaration"/> /
/// <see cref="BestEffortEndpointDeclaration"/>). An endpoint-declaration is what an endpoint IS in every composition —
/// its identity, its topology stance, and its handlers — declared once, as a class, so that production and tests host the
/// same endpoint by construction. What varies per deployment — transport, serializer, discovery, the actual domain
/// database — is the <see cref="IEndpointEnvironment"/>, handed in when the declaration is built.
///
/// The declaration is a blueprint, not the running endpoint: instantiable and inspectable with no container and no
/// lifecycle, and buildable any number of times — one declaration, several endpoint instances across restarts.
///
/// Declaring is overriding: scalar aspects are value overrides (<see cref="RequiredPeers"/>,
/// <see cref="HandlerAvailabilityPatience"/>), and each tessage-handler kind has its own door — a virtual method receiving
/// the minimal registrar for exactly that kind — so a declaration overrides only the doors its endpoint actually serves.
/// Wiring the doors do not cover — store integrations, arbitrary component registration — goes through the tier's
/// general <c>Declare</c> override, which receives the full declaration surface.
///</summary>
public abstract class EndpointDeclaration
{
   ///<summary>The endpoint's human-readable name and durable <see cref="EndpointId"/> — the identity every composition shares.</summary>
   private protected EndpointConfiguration Configuration { get; }

   protected EndpointDeclaration(string name, EndpointId id) => Configuration = new EndpointConfiguration(name, id);

   ///<summary>The peers this endpoint requires — see <see cref="EndpointBuilder{TConcreteBuilder}.RequirePeers"/>. Empty by default.</summary>
   protected virtual IReadOnlyList<EndpointId> RequiredPeers => [];

   ///<summary>The peers this endpoint deliberately keeps nothing for — see<br/>
   /// <see cref="EndpointBuilder{TConcreteBuilder}.DoNotQueueTeventsFor"/>. Empty by default.</summary>
   protected virtual IReadOnlyList<EndpointId> PeersNotQueuedFor => [];

   ///<summary>The endpoint's handler-availability patience — see<br/>
   /// <see cref="EndpointBuilder{TConcreteBuilder}.HandlerAvailabilityPatience"/>. Null — the default — means the endpoint's<br/>
   /// standard patience.</summary>
   protected virtual TimeSpan? HandlerAvailabilityPatience => null;

   ///<summary>Registers the endpoint's components with its container — domain components, query models, and the type-mapping<br/>
   /// requirements of the assemblies they live in (<c>registrar.RequireMappedTypesFromAssemblyContaining&lt;T&gt;()</c>).</summary>
   protected virtual void RegisterComponents(IComponentRegistrar registrar) {}

   ///<summary>The door for tevent subscriptions that do not demand exactly-once delivery — see <see cref="IBestEffortTeventHandlerRegistrar"/>.</summary>
   protected virtual void RegisterBestEffortTeventHandlers(IBestEffortTeventHandlerRegistrar handle) {}

   ///<summary>The door for the navigated tommand kinds — see <see cref="ITypermediaTommandHandlerRegistrar"/>.</summary>
   protected virtual void RegisterTypermediaTommandHandlers(ITypermediaTommandHandlerRegistrar handle) {}

   ///<summary>The door for tuery handlers — see <see cref="ITueryHandlerRegistrar"/>.</summary>
   protected virtual void RegisterTueryHandlers(ITueryHandlerRegistrar handle) {}

   ///<summary>The door for tevent observation — the deliberately transaction-ignoring watch surface; see <see cref="ITeventObservationRegistrar"/>.</summary>
   protected virtual void ObserveTevents(ITeventObservationRegistrar observe) {}

   ///<summary>Declares everything the tiers share onto the builder: the scalar aspects and the shared doors. The tier's own<br/>
   /// <c>BuildOn</c> is the template — environment first, then this, then the tier's own doors, then the general<br/>
   /// <c>Declare</c> override, then the build.</summary>
   private protected void DeclareSharedAspectsOn<TConcreteBuilder>(EndpointBuilder<TConcreteBuilder> builder) where TConcreteBuilder : EndpointBuilder<TConcreteBuilder>
   {
      if(HandlerAvailabilityPatience is { } patience) builder.HandlerAvailabilityPatience(patience);
      builder.RequirePeers([.. RequiredPeers]);
      builder.DoNotQueueTeventsFor([.. PeersNotQueuedFor]);
      builder.RegisterComponents(RegisterComponents);
      builder.RegisterTessageBusHandlers(RegisterBestEffortTeventHandlers);
      builder.RegisterTypermediaHandlers(handle =>
      {
         RegisterTypermediaTommandHandlers(handle);
         RegisterTueryHandlers(handle);
      });
      builder.ObserveTevents(ObserveTevents);
   }
}
