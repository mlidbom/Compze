using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Endpoints.BestEffort;

///<summary>
/// The declaration a concrete <see cref="BestEffortEndpoint"/> inherits — see <see cref="EndpointDeclaration{TIdentity}"/>
/// for the declaration model. This tier deliberately has no exactly-once registration overrides: it wires no durable vertical, so a handler
/// demanding exactly-once delivery cannot be registered through any override — the wiring rule as structure, not assertion.
///</summary>
public abstract class BestEffortEndpointDeclaration<TIdentity> : EndpointDeclaration<TIdentity>, IBestEffortEndpointDeclaration where TIdentity : IEndpointIdentity
{
   ///<summary>The general override: everything the specific registration overrides do not cover — any wiring this base never knows about —<br/>
   /// declared over the endpoint's full <see cref="BestEffortEndpointBuilder"/>.</summary>
   protected virtual void Declare(BestEffortEndpointBuilder endpoint) {}

   ///<inheritdoc />
   public BestEffortEndpoint Build(IContainerBuilder containerBuilder, IEndpointEnvironment environment)
   {
      var builder = new BestEffortEndpointBuilder(containerBuilder, Configuration);
      environment.Configure(builder);
      ConfigureSharedAspects(builder);
      Declare(builder);
      return builder.Build();
   }
}
