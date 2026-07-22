namespace Compze.Tessaging.Endpoints;

///<summary>An endpoint's identity as a type: the compiler-enforced static <see cref="Name"/> and <see cref="Id"/> an<br/>
/// <see cref="EndpointDeclaration{TIdentity}"/> builds under, always addressable without an instance —<br/>
/// <c>SomeEndpointDeclaration.Id</c> — which is what cross-declaration references (<c>RequiredPeers</c>) speak. Usually<br/>
/// implemented by the concrete declaration itself, beside its declaration base; a standalone identity type works too, for<br/>
/// identities that live apart from their declarations (shared identity/contract code).</summary>
public interface IEndpointIdentity
{
   ///<summary>The endpoint's human-readable name.</summary>
   static abstract string Name { get; }

   ///<summary>The endpoint's durable identity: addresses are per-instance and change across restarts; the <see cref="EndpointId"/> never does.</summary>
   static abstract EndpointId Id { get; }
}
