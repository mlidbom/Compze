using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageBus;

namespace Compze.Tessaging.Endpoints.ExactlyOnce;

///<summary>
/// The declaration a concrete <see cref="ExactlyOnceEndpoint"/> inherits — see <see cref="EndpointDeclaration{TIdentity}"/>
/// for the declaration model. This tier adds the two exactly-once doors: handlers for exactly-once tommands
/// (<see cref="RegisterExactlyOnceTommandHandlers"/>) and for tevent subscriptions demanding exactly-once delivery
/// (<see cref="RegisterExactlyOnceTeventHandlers"/>). The best-effort tier deliberately lacks both — an endpoint that
/// cannot honor a guarantee has no door to declare handlers demanding it.
///</summary>
public abstract class ExactlyOnceEndpointDeclaration<TIdentity> : EndpointDeclaration<TIdentity>, IExactlyOnceEndpointDeclaration where TIdentity : IEndpointIdentity
{
   ///<summary>The door for exactly-once tommand handlers — see <see cref="IExactlyOnceTommandHandlerRegistrar"/>.</summary>
   protected virtual void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) {}

   ///<summary>The door for tevent subscriptions demanding exactly-once delivery — see <see cref="IExactlyOnceTeventHandlerRegistrar"/>.</summary>
   protected virtual void RegisterExactlyOnceTeventHandlers(IExactlyOnceTeventHandlerRegistrar handle) {}

   ///<summary>The general door: everything the named doors do not cover — store integrations (a tevent store's<br/>
   /// <c>RegisterTeventStore</c>, a document db's <c>RegisterDocumentDb</c>) and any wiring this base never knows about —<br/>
   /// declared over the full declaration surface.</summary>
   protected virtual void Declare(ExactlyOnceEndpointBuilder endpoint) {}

   ///<inheritdoc />
   public ExactlyOnceEndpoint BuildOn(IContainerBuilder containerBuilder, IEndpointEnvironment environment)
   {
      var builder = new ExactlyOnceEndpointBuilder(containerBuilder, Configuration);
      environment.DeclareOn(builder);
      environment.DeclareDomainDatabaseOn(builder);
      DeclareSharedAspectsOn(builder);
      builder.RegisterTessageBusHandlers(handle =>
      {
         RegisterExactlyOnceTommandHandlers(handle);
         RegisterExactlyOnceTeventHandlers(handle);
      });
      Declare(builder);
      return builder.Build();
   }
}
