namespace Compze.Internals.Transport.NamedPipes;

///<summary>One communication style's request handling, contributed to the endpoint's named-pipe transport server<br/>
/// (<see cref="NamedPipeEndpointTransportServer"/>): which <see cref="NamedPipeTransportRequestKind"/>s the style serves, and how.<br/>
/// Registered as a component set member (<c>Singleton.ForSet</c>) by the style's named-pipe transport registration; the server<br/>
/// resolves the whole set and serves the union.</summary>
///<remarks><see cref="NamedPipeTransportRequestKind.InfrastructureQuery"/> is never contributed — the server itself answers<br/>
/// infrastructure queries, because every endpoint serves discovery no matter what it speaks.</remarks>
public interface INamedPipeRequestHandlerContribution
{
   ///<summary>The handler for each request kind this communication style serves.</summary>
   IReadOnlyDictionary<NamedPipeTransportRequestKind, Func<NamedPipeTransportRequest, Task<string>>> RequestHandlers { get; }
}
