using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Threading;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport.FramedConnections;

///<summary>The server side of the framed-connection transports: serves every connection its <see cref="IConnectionListener"/><br/>
/// accepts. On each connection it reads <see cref="TransportRequest"/>s, dispatches each through the dispatch function it is<br/>
/// given, and answers with the handler's payload, or, if the handler threw, with an error response the client rethrows.<br/>
/// The no-web-stack counterpart of the endpoint transport's ASP.NET Core server.</summary>
///<remarks>A handler returns the serialized response payload; transport-level concerns (framing, connection lifecycle,<br/>
/// routing exceptions back to the client) all live here, so handlers contain only "deserialize, execute, serialize".</remarks>
///<remarks>Each accepted connection is served on its own task, for as long as the client keeps it open:<br/>
/// <see cref="ServeConnectionAsync"/> answers request after request until the client closes its end, so clients may hold<br/>
/// connections open and converse over them indefinitely without reconnecting.</remarks>
sealed class FramedConnectionServer : IAsyncDisposable
{
   readonly IConnectionListener _listener;
   readonly Func<TransportRequest, Task<string>> _dispatchRequest;
   readonly CancellationTokenSource _cancellationSource = new();
   readonly IMonitor _monitor = IMonitor.New();
   readonly HashSet<Stream> _openConnections = [];
   readonly HashSet<Task> _connectionsBeingServed = [];
   bool _isListening;

   public FramedConnectionServer(IConnectionListener listener, Func<TransportRequest, Task<string>> dispatchRequest)
   {
      _listener = listener;
      _dispatchRequest = dispatchRequest;
   }

   ///<summary>The address clients connect to. Read it once the server has started, see <see cref="IConnectionListener.Address"/>.</summary>
   public EndpointAddress Address => _listener.Address;

   public void Start()
   {
      State.Assert(!_isListening);
      _isListening = true;
      _listener.StartListening(ServeUntilDoneThenDispose);
      this.Log().Info($"Transport server listening at {Address.Uri}");
   }

   public async Task StopAsync()
   {
      if(!_isListening) return;
      var address = Address;
      await _cancellationSource.CancelAsync().caf();

      //First, so that every connection the listener hands over is tracked by the time the open connections are disposed below.
      await _listener.StopListeningAsync().caf();

      //Disposing the open connections interrupts the reads of the connections being served that the token has not yet reached,
      //so the serve tasks unwind on the cancellation.
      var openConnections = _monitor.Locked<Stream[]>(() => [.._openConnections]);
      foreach(var connection in openConnections) await connection.DisposeAsync().caf();

      var connectionsBeingServed = _monitor.Locked<Task[]>(() => [.._connectionsBeingServed]);
      await Task.WhenAll(connectionsBeingServed).caf();

      _isListening = false;
      this.Log().Info($"Transport server at {address.Uri} stopped");
   }

   public async ValueTask DisposeAsync()
   {
      await StopAsync().caf();
      await _listener.DisposeAsync().caf();
      _cancellationSource.Dispose();
   }

   ///<summary>Takes ownership of a connection the listener accepted: serves it on a task of its own, and disposes it when the serving ends.</summary>
   void ServeUntilDoneThenDispose(Stream connection)
   {
      _monitor.Locked(() => _openConnections.Add(connection));
      var serving = ServeConnectionThenDisposeAsync(connection);
      //Tracked so StopAsync can wait for in-flight serves to unwind; the continuation removes it whether it completed before
      //or after this add, so the set never leaks a finished serve nor grows without bound under sustained load.
      _monitor.Locked(() => _connectionsBeingServed.Add(serving));
      serving.ContinueWithCE(_ => _monitor.Locked(() => _connectionsBeingServed.Remove(serving)));
   }

   async Task ServeConnectionThenDisposeAsync(Stream connection)
   {
      try
      {
         await ServeConnectionAsync(connection).caf();
      }
      finally
      {
         _monitor.Locked(() => _openConnections.Remove(connection));
         await connection.DisposeAsync().caf();
      }
   }

   async Task ServeConnectionAsync(Stream connection)
   {
      try
      {
         while(!_cancellationSource.IsCancellationRequested)
         {
            TransportRequest request;
            try
            {
               request = await ExchangeFraming.ReadRequestAsync(connection, _cancellationSource.Token).caf();
            }
            catch(EndOfStreamException) //The client closed the connection: the normal end of every conversation, signaled by the connection itself.
            {
               return;
            }

            string responsePayload;
            try
            {
               responsePayload = await _dispatchRequest(request).caf();
            }
#pragma warning disable CA1031 //We catch all exceptions here to route them back to the client, exactly as the HTTP transport's controllers do.
            catch(Exception exception)
#pragma warning restore CA1031
            {
               this.Log().Warning(exception, $"Exception handling {request.Kind}");
               await ExchangeFraming.WriteErrorResponseAsync(connection, exception.GetType().FullName!, exception.ToString(), _cancellationSource.Token).caf();
               continue;
            }

            await ExchangeFraming.WriteSuccessResponseAsync(connection, responsePayload, _cancellationSource.Token).caf();
         }
      }
      catch(OperationCanceledException) {} //Stopping: in-flight conversations are interrupted by our own cancellation token.
      catch(ObjectDisposedException) {} //Stopping: StopAsync disposes open connections to interrupt whatever the token has not yet reached.
      catch(IOException) {} //The client process vanished mid-conversation. A peer disappearing is a real runtime condition no server can prevent; the client side is the one that observes and handles the failure.
   }
}
