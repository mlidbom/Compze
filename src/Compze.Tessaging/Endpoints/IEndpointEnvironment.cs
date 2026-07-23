using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Tessaging.Endpoints;

///<summary>
/// The environment an endpoint runs in — everything a deployment decides and an <see cref="EndpointDeclaration{TIdentity}"/>
/// deliberately does not: the transport protocol, the serializer, participation in discovery, and — for the exactly-once
/// tier — the binding of the endpoint to an actual domain database. One environment serves every endpoint of a process:
/// a host created with one applies it to every declaration it builds
/// (<see cref="IEndpointHost.RegisterEndpoint(IExactlyOnceEndpointDeclaration)"/>), and building without a host hands it to
/// the declaration directly (<see cref="IExactlyOnceEndpointDeclaration.Build"/>). The production composition of an
/// application implements this once; the testing environment lives inside the testing host. An endpoint needing the shared
/// environment plus something extra runs in a decorating implementation wrapping the host's — delegate to the wrapped
/// environment, add the extra declaration — registered through
/// <see cref="IEndpointHost.RegisterEndpoint(IExactlyOnceEndpointDeclaration, IEndpointEnvironment)"/>.
///</summary>
public interface IEndpointEnvironment
{
   ///<summary>Declares this environment's choices on the endpoint being built — the transport protocol, the serializer,<br/>
   /// discovery participation — on the same <see cref="EndpointBuilder"/> everything else declares on.</summary>
   void Configure(EndpointBuilder endpointBuilder);

   ///<summary>Binds an exactly-once endpoint to the actual domain database it joins in this environment — the engine and<br/>
   /// connection string behind the endpoint's durable vertical. Called only when building the exactly-once tier: the<br/>
   /// best-effort tier persists nothing.</summary>
   void ConfigureDomainDatabase(ExactlyOnceEndpointBuilder endpointBuilder);
}
