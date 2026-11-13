using System.Net.Sockets;

namespace Compze.Tessaging._private.Transport.Tcp;

///<summary>A connection of the TCP transport as both of its ends set it up: the byte stream over a connected socket, with<br/>
/// Nagle's algorithm off and TCP keepalive on.</summary>
///<remarks>Nagle's algorithm holds a small send back, waiting to combine it with the next one. A frame is one send and is<br/>
/// followed by a wait for the other side's frame, so there is never a next send to combine with: the algorithm could only<br/>
/// delay frames.</remarks>
///<remarks>Keepalive is what ends a connection whose peer disappeared without closing it: its machine was paused or<br/>
/// turned off, or the network between them was cut. Nothing arrives then, and a read that waits for the peer's next frame<br/>
/// is bounded by no timeout, because an exchange lasts as long as its handler runs. With keepalive the operating system<br/>
/// probes a connection that has been silent for <see cref="SecondsOfSilenceBeforeTheFirstProbe"/> seconds, and fails it,<br/>
/// and the read with it, once <see cref="UnansweredProbesBeforeTheConnectionFails"/> probes in a row went unanswered. A<br/>
/// peer that is busy handling a request answers the probes: they are answered by its operating system, not by its handler.</remarks>
static class TcpConnection
{
   const int SecondsOfSilenceBeforeTheFirstProbe = 5;
   const int SecondsBetweenProbes = 1;
   const int UnansweredProbesBeforeTheConnectionFails = 5;

   ///<summary>The transport's connection over <paramref name="connectedSocket"/>, which the returned stream owns: disposing<br/>
   /// the stream closes the socket.</summary>
   internal static Stream StreamOver(Socket connectedSocket)
   {
      try
      {
         connectedSocket.NoDelay = true;
         connectedSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
         connectedSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, SecondsOfSilenceBeforeTheFirstProbe);
         connectedSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, SecondsBetweenProbes);
         connectedSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, UnansweredProbesBeforeTheConnectionFails);
         return new NetworkStream(connectedSocket, ownsSocket: true);
      }
      catch //Resource cleanup, not handling: no stream came to own the socket, so nothing else will close it.
      {
         connectedSocket.Dispose();
         throw;
      }
   }
}
