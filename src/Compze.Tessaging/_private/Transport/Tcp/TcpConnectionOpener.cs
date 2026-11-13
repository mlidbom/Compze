using System.Net;
using System.Net.Sockets;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._private.Transport.FramedConnections;

namespace Compze.Tessaging._private.Transport.Tcp;

///<summary>The TCP transport's <see cref="IConnectionOpener"/>: connects a socket to the server listening at a<br/>
/// <see cref="TcpAddress"/>, and sets the connection up as <see cref="TcpConnection"/> describes.</summary>
///<remarks>A TCP connect is asynchronous and holds no thread while it waits. It needs a timeout of its own all the same:<br/>
/// a connect to a machine that is not there is answered by nothing, and the operating system waits far longer than the<br/>
/// transport should before it gives up.</remarks>
class TcpConnectionOpener : IConnectionOpener
{
   public string TransportName => "TCP";

   public async Task<Stream> OpenConnectionAsync(EndpointAddress address, TimeSpan timeout, CancellationToken cancellationToken)
   {
#pragma warning disable CA2000 //The socket is owned from here on by the stream StreamOver returns, which closes it when disposed, and StreamOver closes it itself if it fails: an ownership the analyzer cannot follow into NetworkStream.
      var connectedSocket = await SocketConnectedToAsync(TcpAddress.IPEndPointFrom(address), timeout, cancellationToken).caf();
#pragma warning restore CA2000
      return TcpConnection.StreamOver(connectedSocket);
   }

   static async Task<Socket> SocketConnectedToAsync(IPEndPoint serverListensAt, TimeSpan timeout, CancellationToken cancellationToken)
   {
      var socket = new Socket(serverListensAt.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      try
      {
         using var timeoutOrCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
         timeoutOrCancellation.CancelAfter(timeout);
         await socket.ConnectAsync(serverListensAt, timeoutOrCancellation.Token).caf();
         return socket;
      }
      catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested) //Our own timeout ended the connect, not the caller: say so in the terms of IConnectionOpener's contract.
      {
         socket.Dispose();
         throw new TimeoutException($"No TCP connection to {serverListensAt} was made within {timeout}.");
      }
      catch //Resource cleanup, not handling: the socket never connected, so it is handed to nobody and nothing else will close it.
      {
         socket.Dispose();
         throw;
      }
   }
}
