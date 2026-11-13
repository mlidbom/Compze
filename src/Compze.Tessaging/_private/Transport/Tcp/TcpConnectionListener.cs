using System.Net;
using System.Net.Sockets;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._private.Transport.FramedConnections;

namespace Compze.Tessaging._private.Transport.Tcp;

///<summary>The TCP transport's <see cref="IConnectionListener"/>: listens on one IP address of this machine, at a port the<br/>
/// operating system picks when listening starts, and sets every connection it accepts up as <see cref="TcpConnection"/><br/>
/// describes.</summary>
///<remarks>One accept loop is enough: connections that arrive while it hands one off wait in the listening socket's own<br/>
/// queue, where a named pipe needs a listening instance per connection.</remarks>
sealed class TcpConnectionListener : IConnectionListener
{
   readonly IPAddress _listenOn;
   readonly CancellationTokenSource _cancellationSource = new();
   Socket? _listeningSocket;
   EndpointAddress? _address;
   Task? _acceptLoop;

   internal TcpConnectionListener(IPAddress listenOn) => _listenOn = listenOn;

   ///<summary>Known once listening has started: the port is the operating system's choice at that moment.</summary>
   public EndpointAddress Address => _address._assert().NotNull();

   public void StartListening(Action<Stream> serveConnection)
   {
      State.Assert(_listeningSocket is null);
      _listeningSocket = new Socket(_listenOn.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      //Port 0 asks the operating system for a free port, so every server start listens at a fresh address.
      _listeningSocket.Bind(new IPEndPoint(_listenOn, port: 0));
      _listeningSocket.Listen();
      _address = TcpAddress.CreateAddressOfServerListeningAt((IPEndPoint)_listeningSocket.LocalEndPoint._assert().NotNull());
      _acceptLoop = TaskCE.Run(() => AcceptLoopAsync(serveConnection));
   }

   public async Task StopListeningAsync()
   {
      if(_acceptLoop is null) return;
      await _cancellationSource.CancelAsync().caf();
      //Closing the listening socket interrupts an accept the token has not yet reached, so the loop unwinds on the cancellation.
      ListeningSocket.Dispose();
      await _acceptLoop.caf();
      _acceptLoop = null;
   }

   public async ValueTask DisposeAsync()
   {
      await StopListeningAsync().caf();
      //Also closes a socket that was created but never came to listen, because binding it or listening on it failed.
      _listeningSocket?.Dispose();
      _cancellationSource.Dispose();
   }

   Socket ListeningSocket => _listeningSocket._assert().NotNull();

   async Task AcceptLoopAsync(Action<Stream> serveConnection)
   {
      while(!_cancellationSource.IsCancellationRequested)
      {
         var connection = await AcceptConnectionOrNullWhenStoppingAsync().caf();
         if(connection is null) return;
         serveConnection(connection);
      }
   }

   ///<summary>Waits for a client to connect, and returns the connection, which the caller owns. Null when the listener<br/>
   /// stopped while it waited.</summary>
   async Task<Stream?> AcceptConnectionOrNullWhenStoppingAsync()
   {
      Socket connectedSocket;
      try
      {
         //review: an accept that fails while the listener is not stopping ends the accept loop, and the server then accepts
         //   nothing more: the failure shows only when the listener stops. Sockets document accept failures that concern
         //   one incoming connection alone, such as a client that reset its connect before it was accepted. None has been
         //   seen here. If one is, it must be characterized, and the loop kept alive past it.
         connectedSocket = await ListeningSocket.AcceptAsync(_cancellationSource.Token).caf();
      }
#pragma warning disable CA1031 //The filter makes this narrow: only while stopping — the canceled accept / closed listening socket is the shutdown signal itself, not a failure.
      catch(Exception) when(_cancellationSource.IsCancellationRequested)
#pragma warning restore CA1031
      {
         return null;
      }

      return TcpConnection.StreamOver(connectedSocket);
   }
}
