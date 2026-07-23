using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Endpoints.ExactlyOnce;

///<summary>The non-generic face of an <see cref="ExactlyOnceEndpointDeclaration{TIdentity}"/> — what a host takes<br/>
/// (<see cref="IEndpointHost.RegisterEndpoint(IExactlyOnceEndpointDeclaration)"/>), since the generic base exists only to<br/>
/// reach the identity type's statics.</summary>
public interface IExactlyOnceEndpointDeclaration
{
   ///<summary>Builds this declaration into a running-ready <see cref="ExactlyOnceEndpoint"/>: the template that guarantees<br/>
   /// every composition the same setup order — the environment declares its choices and the domain database binding, the<br/>
   /// declaration's aspects and doors follow, the general <see cref="ExactlyOnceEndpointDeclaration{TIdentity}.Declare"/><br/>
   /// override last, and the build closes the roster. Called by the host that owns the endpoint<br/>
   /// (<see cref="IEndpointHost.RegisterEndpoint(IExactlyOnceEndpointDeclaration)"/>) or directly — an endpoint is<br/>
   /// first-class and needs no host.</summary>
   ExactlyOnceEndpoint Build(IContainerBuilder containerBuilder, IEndpointEnvironment environment);
}
