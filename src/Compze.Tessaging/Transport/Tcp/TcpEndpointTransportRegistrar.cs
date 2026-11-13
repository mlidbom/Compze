using System.Net;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging._internal.Transport;
using Compze.Tessaging._internal.Transport.Advertisement;
using Compze.Tessaging._internal.Transport.Tcp;
using Compze.Tessaging._private.Transport;
using Compze.Tessaging._private.Transport.Tcp;

namespace Compze.Tessaging.Transport.Tcp;

public static class TcpEndpointTransportRegistrar
{
   ///<summary>Declares the endpoint's transport protocol: TCP — the protocol that reaches other machines with no web stack.<br/>
   /// Registers the TCP endpoint transport client (<see cref="IEndpointTransportClient"/>), the endpoint-discovery query<br/>
   /// transport that runs on it, and the endpoint's one transport server (<see cref="IEndpointTransportServer"/>) serving every<br/>
   /// remotable capability's contributed request handlers.</summary>
   ///<remarks>The server listens on <paramref name="listenOn"/>, at a port the operating system picks each time it starts,<br/>
   /// and that address and port are the endpoint's address, the one it announces. So <paramref name="listenOn"/> must be an<br/>
   /// address of this machine that the endpoints it converses with can connect to: <see cref="IPAddress.Loopback"/> for<br/>
   /// endpoints on this machine only, the machine's address on a network for endpoints on that network. An address that<br/>
   /// stands for every network interface, such as <see cref="IPAddress.Any"/>, is refused: nothing can connect to it.</remarks>
   ///<remarks>The transport authenticates nobody and encrypts nothing: whatever can connect to the address can converse<br/>
   /// with the endpoint, and whatever sits between two endpoints can read what they say. Listen only on networks where<br/>
   /// that is acceptable.</remarks>
   public static IComponentRegistrar TcpEndpointTransport(this IComponentRegistrar registrar, IPAddress listenOn)
   {
      Argument.Assert(!listenOn.Equals(IPAddress.Any) && !listenOn.Equals(IPAddress.IPv6Any),
                      () => $"The TCP transport cannot listen on {listenOn}: that stands for every network interface of this machine, and the address listened on is the address the endpoint announces, which nothing could connect to. Declare the one address of this machine the endpoint is to be reached at.");

      return registrar.TcpEndpointTransportClientIfNotRegistered()
                      .EndpointInformationQueryTransportIfNotRegistered()
                      .TcpEndpointTransportServerIfNotRegistered(listenOn);
   }

   extension(Endpoints.EndpointBuilder @this)
   {
      ///<summary>Declares the endpoint's transport protocol: TCP — the protocol that reaches other machines with no web stack.<br/>
      /// See <see cref="TcpEndpointTransport(IComponentRegistrar, IPAddress)"/>, to which this delegates.</summary>
      public Endpoints.EndpointBuilder TcpEndpointTransport(IPAddress listenOn) => @this.TransportProtocol(registrar => registrar.TcpEndpointTransport(listenOn));
   }
}
