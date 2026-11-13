using Compze.Tessaging.Endpoints.Discovery;

namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>How a framed-connection transport's server is reached: listens at one <see cref="EndpointAddress"/> and hands<br/>
/// every connection a client opens to it, a duplex byte stream, to the <see cref="FramedConnectionServer"/> that serves it.<br/>
/// Each framed-connection transport supplies its own.</summary>
///<remarks>The client's side of the same connection comes from the transport's <see cref="IConnectionOpener"/>.</remarks>
interface IConnectionListener : IAsyncDisposable
{
   ///<summary>The address clients open connections to. Read it once listening has started: a transport whose address is<br/>
   /// decided when it starts listening has none before.</summary>
   EndpointAddress Address { get; }

   ///<summary>Starts listening, and returns at once. From here on every accepted connection is handed to<br/>
   /// <paramref name="serveConnection"/>, which takes ownership of it, until <see cref="StopListeningAsync"/>.</summary>
   void StartListening(Action<Stream> serveConnection);

   ///<summary>Stops accepting connections. Completes when no further connection will be handed over.</summary>
   Task StopListeningAsync();
}
