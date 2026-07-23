using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Tessaging.Endpoints.BestEffort;

///<summary>The non-generic face of a <see cref="BestEffortEndpointDeclaration{TIdentity}"/> — what a host takes<br/>
/// (<see cref="IEndpointHost.RegisterEndpoint(IBestEffortEndpointDeclaration)"/>), since the generic base exists only to<br/>
/// reach the identity type's statics.</summary>
public interface IBestEffortEndpointDeclaration
{
   ///<summary>Builds this declaration into a running-ready <see cref="BestEffortEndpoint"/> — see<br/>
   /// <see cref="IExactlyOnceEndpointDeclaration.Build"/>, whose template this mirrors minus the domain database: this<br/>
   /// tier persists nothing.</summary>
   BestEffortEndpoint Build(IContainerBuilder containerBuilder, IEndpointEnvironment environment);
}
