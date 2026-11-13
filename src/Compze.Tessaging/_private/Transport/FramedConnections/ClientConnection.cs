namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>A connection a framed-connection transport's client has opened to a server (<see cref="IConnectionOpener"/>):<br/>
/// the duplex byte <see cref="Stream"/> its exchanges travel over, and what the transport can tell about the connection<br/>
/// while no exchange is using it (<see cref="HasEndedWhileIdle"/>).</summary>
sealed class ClientConnection : IDisposable, IAsyncDisposable
{
   readonly Func<bool> _hasEndedWhileIdle;

   ///<param name="stream">The connection's byte stream, which the connection owns from here on: disposing the connection disposes it.</param>
   ///<param name="hasEndedWhileIdle">How the transport tells that this connection, while idle, has ended: see <see cref="HasEndedWhileIdle"/>.</param>
   internal ClientConnection(Stream stream, Func<bool> hasEndedWhileIdle)
   {
      Stream = stream;
      _hasEndedWhileIdle = hasEndedWhileIdle;
   }

   internal Stream Stream { get; }

   ///<summary>Whether the connection is known to have ended while no exchange was using it: the server closed it, or the<br/>
   /// operating system failed it because the server's machine stopped answering. Only meaningful while the connection is<br/>
   /// idle, between complete exchanges.</summary>
   ///<remarks><see cref="FramedConnectionPool"/> asks before it uses an idle connection again. An exchange started on a<br/>
   /// connection that has already ended fails, although the server may be reachable again: a paused machine resumed, a<br/>
   /// cut network restored. Known to have ended, the connection is discarded instead and the exchange gets a fresh one.</remarks>
   ///<remarks>False does not promise that the connection works: a server whose machine vanished moments ago is not yet<br/>
   /// known to be gone, and the exchange that finds out fails.</remarks>
   internal bool HasEndedWhileIdle => _hasEndedWhileIdle();

   public void Dispose() => Stream.Dispose();

   public ValueTask DisposeAsync() => Stream.DisposeAsync();
}
