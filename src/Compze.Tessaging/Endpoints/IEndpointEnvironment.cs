using Compze.Tessaging.Endpoints.ExactlyOnce;

namespace Compze.Tessaging.Endpoints;

///<summary>
/// The environment an endpoint runs in — everything a deployment decides and an <see cref="EndpointDeclaration"/>
/// deliberately does not: the transport protocol, the serializer, participation in discovery, and — for the exactly-once
/// tier — the binding of the endpoint to an actual domain database. One environment serves every endpoint of a process:
/// a host created with one applies it to every declaration it builds
/// (<see cref="IEndpointHost.RegisterEndpoint(ExactlyOnceEndpointDeclaration)"/>), and building without a host hands it to
/// the declaration directly (<see cref="ExactlyOnceEndpointDeclaration.BuildOn"/>). The production composition of an
/// application implements this once; the testing environment lives inside the testing host.
///</summary>
public interface IEndpointEnvironment
{
   ///<summary>Declares this environment's choices on the endpoint being built — the transport protocol, the serializer,<br/>
   /// discovery participation — through the same declaration surface every composition uses.</summary>
   void DeclareOn<TConcreteBuilder>(EndpointBuilder<TConcreteBuilder> endpointBuilder) where TConcreteBuilder : EndpointBuilder<TConcreteBuilder>;

   ///<summary>Binds an exactly-once endpoint to the actual domain database it joins in this environment — the engine and<br/>
   /// connection string behind the endpoint's durable vertical. Called only when building the exactly-once tier: the<br/>
   /// best-effort tier persists nothing.</summary>
   void DeclareDomainDatabaseOn(ExactlyOnceEndpointBuilder endpointBuilder);
}
