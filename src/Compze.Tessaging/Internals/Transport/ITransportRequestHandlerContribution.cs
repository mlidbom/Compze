namespace Compze.Tessaging.Internals.Transport;

///<summary>One communication style's request handling, contributed to the endpoint's one transport server<br/>
/// (<see cref="IEndpointTransportServer"/>): which <see cref="TransportRequestKind"/>s the style serves, and how.<br/>
/// Registered as a component set member (<c>Singleton.ForSet</c>) by the style's transport registration; the server<br/>
/// resolves the whole set and serves the union.</summary>
///<remarks><see cref="TransportRequestKind.EndpointDiscoveryQuery"/> is never contributed — the server itself answers<br/>
/// endpoint-discovery queries, because every endpoint serves discovery no matter what it speaks.</remarks>
interface ITransportRequestHandlerContribution
{
   ///<summary>The handler for each request kind this communication style serves.</summary>
   IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> RequestHandlers { get; }
}
