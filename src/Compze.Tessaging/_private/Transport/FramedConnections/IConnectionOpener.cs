using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>How a framed-connection transport's client reaches a server: opens a <see cref="ClientConnection"/> to the<br/>
/// server listening at an <see cref="EndpointAddress"/>. Each framed-connection transport supplies its own; what travels<br/>
/// over the connection once it is open is the same for all of them (<see cref="ExchangeFraming"/>).</summary>
///<remarks>The server's side of the same connection comes from the transport's <see cref="IConnectionListener"/>.</remarks>
interface IConnectionOpener
{
   ///<summary>The transport's name as failure messages spell it, such as <c>named-pipe</c>.</summary>
   string TransportName { get; }

   ///<summary>Opens a connection to the server listening at <paramref name="address"/>, which must be an address of this<br/>
   /// transport. Throws an <see cref="EndpointUnreachableException"/> when no connection can be made: nothing answers<br/>
   /// within <paramref name="timeout"/>, or the attempt fails outright. The caller owns the connection it gets and<br/>
   /// disposes it.</summary>
   Task<ClientConnection> OpenConnectionAsync(EndpointAddress address, TimeSpan timeout, CancellationToken cancellationToken);
}
