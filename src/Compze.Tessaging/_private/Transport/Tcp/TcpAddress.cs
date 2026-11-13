using System.Net;
using Compze.Contracts;
using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging._private.Transport.Tcp;

///<summary>The TCP transport's <see cref="EndpointAddress"/> scheme: <c>compze.tcp://&lt;IP address&gt;:&lt;port&gt;</c>.<br/>
/// The IP address is the one the server was declared to listen on; the port is one the operating system picks each time a<br/>
/// server starts listening, so a server start gets a fresh address, as the named-pipe transport's fresh pipe names give it.</summary>
static class TcpAddress
{
   ///<summary>The URI scheme identifying an address served by the TCP transport.</summary>
   const string Scheme = "compze.tcp";

   ///<summary>The <see cref="EndpointAddress"/> a server listening at <paramref name="listeningAt"/> publishes for clients to connect to.</summary>
   public static EndpointAddress CreateAddressOfServerListeningAt(IPEndPoint listeningAt) => new(new Uri($"{Scheme}://{listeningAt}"));

   ///<summary>The IP address and port to connect to for <paramref name="address"/>. Asserts that the address belongs to this transport.</summary>
   public static IPEndPoint IPEndPointFrom(EndpointAddress address)
   {
      Argument.Assert(address.Uri.Scheme == Scheme);
      return new IPEndPoint(IPAddress.Parse(address.Uri.DnsSafeHost), address.Uri.Port);
   }
}
