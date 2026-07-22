using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Endpoints.BestEffort;

///<summary>
/// The declaration a concrete <see cref="BestEffortEndpoint"/> inherits — see <see cref="EndpointDeclaration{TIdentity}"/>
/// for the declaration model. This tier deliberately has no exactly-once doors: it wires no durable vertical, so a handler
/// demanding exactly-once delivery has no door to be declared through — the wiring rule as structure, not assertion.
///</summary>
public abstract class BestEffortEndpointDeclaration<TIdentity> : EndpointDeclaration<TIdentity>, IBestEffortEndpointDeclaration where TIdentity : IEndpointIdentity
{
   ///<summary>The general door: everything the named doors do not cover — any wiring this base never knows about —<br/>
   /// declared over the full declaration surface.</summary>
   protected virtual void Declare(BestEffortEndpointBuilder endpoint) {}

   ///<inheritdoc />
   public BestEffortEndpoint BuildOn(IContainerBuilder containerBuilder, IEndpointEnvironment environment)
   {
      var builder = new BestEffortEndpointBuilder(containerBuilder, Configuration);
      environment.DeclareOn(builder);
      DeclareSharedAspectsOn(builder);
      Declare(builder);
      return builder.Build();
   }
}
