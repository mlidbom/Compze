using System.IO.Pipes;
using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Threading;
using Compze.Tessaging._internal.Transport;

namespace Compze.Tessaging._private.Transport.NamedPipes;

///<summary>The server side of the named-pipe transport: listens on a freshly named pipe, reads<br/>
/// <see cref="TransportRequest"/>s, dispatches each through the dispatch function it is given, and answers with the<br/>
/// handler's payload — or, if the handler threw, with an error response the client rethrows. The same-machine, no-web-stack<br/>
/// counterpart of the endpoint transport's ASP.NET Core server.</summary>
///<remarks>A handler returns the serialized response payload; transport-level concerns (framing, connection lifecycle,<br/>
/// routing exceptions back to the client) all live here, so handlers contain only "deserialize, execute, serialize".</remarks>
///<remarks>A fixed set of listening instances stays ready to accept: each accepts a connection, hands it off to be served on<br/>
/// its own task, and immediately re-arms to accept the next. A connection is served for as long as the client keeps it open —<br/>
/// <see cref="ServeConnectionAsync"/> answers request after request until the client closes its end — so clients may hold<br/>
/// connections open and converse over them indefinitely without reconnecting.</remarks>
sealed class NamedPipeTransportServer : IAsyncDisposable
{
   static readonly int ListeningInstanceCount = Math.Max(Environment.ProcessorCount * 2, 8);

   readonly Func<TransportRequest, Task<string>> _dispatchRequest;
   readonly string _pipeName = NamedPipeAddress.NewUniquePipeName();
   readonly CancellationTokenSource _cancellationSource = new();
   readonly IMonitor _monitor = IMonitor.New();
   readonly HashSet<NamedPipeServerStream> _openStreams = [];
   readonly HashSet<Task> _connectionsBeingServed = [];
   Task[]? _listenerLoops;

   public NamedPipeTransportServer(Func<TransportRequest, Task<string>> dispatchRequest) => _dispatchRequest = dispatchRequest;

   ///<summary>The address clients connect to; fixed at construction, listening starts at <see cref="StartAsync"/>.</summary>
   public EndpointAddress Address => NamedPipeAddress.CreateLocalAddressForPipe(_pipeName);

   public Task StartAsync()
   {
      State.Assert(_listenerLoops is null);
      _listenerLoops = Enumerable.Range(0, ListeningInstanceCount).Select(_ => TaskCE.Run(ListenerLoopAsync)).ToArray();
      this.Log().Info($"Named-pipe transport server listening at {Address.Uri}");
      return Task.CompletedTask;
   }

   public async Task StopAsync()
   {
      if(_listenerLoops is null) return;
      await _cancellationSource.CancelAsync().caf();

      //Disposing the open streams interrupts both the listener instances waiting for a connection and the reads of any
      //connections currently being served, so both the loops and the serve tasks unwind on the cancellation.
      NamedPipeServerStream[] openStreams = [];
      _monitor.Locked(() => openStreams = _openStreams.ToArray());
      foreach(var stream in openStreams) await stream.DisposeAsync().caf();

      await Task.WhenAll(_listenerLoops).caf();

      Task[] connectionsBeingServed = [];
      _monitor.Locked(() => connectionsBeingServed = _connectionsBeingServed.ToArray());
      await Task.WhenAll(connectionsBeingServed).caf();

      _listenerLoops = null;
      this.Log().Info($"Named-pipe transport server at {Address.Uri} stopped");
   }

   public async ValueTask DisposeAsync()
   {
      await StopAsync().caf();
      _cancellationSource.Dispose();
   }

   async Task ListenerLoopAsync()
   {
      while(!_cancellationSource.IsCancellationRequested)
      {
         var stream = CreateAndTrackListeningInstance();
         try
         {
            await stream.WaitForConnectionAsync(_cancellationSource.Token).caf();
         }
#pragma warning disable CA1031 //The filter makes this narrow: only while stopping — the canceled wait / disposed listener is the shutdown signal itself, not a failure.
         catch(Exception) when(_cancellationSource.IsCancellationRequested)
#pragma warning restore CA1031
         {
            DisposeAndUntrack(stream);
            return;
         }

         //Hand the accepted connection off to be served on its own task and loop back at once to accept the next: this
         //instance never blocks on a handler, so every listening instance stays ready to accept even under a burst of clients.
         ServeUntilDoneThenDispose(stream);
      }
   }

   NamedPipeServerStream CreateAndTrackListeningInstance()
   {
#pragma warning disable CA2000 //Ownership is tracked: the stream is disposed by ServeUntilDoneThenDispose, or, if we are stopping, by StopAsync / the loop's cancellation catch.
      var stream = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
#pragma warning restore CA2000
      _monitor.Locked(() => _openStreams.Add(stream));
      return stream;
   }

   void ServeUntilDoneThenDispose(NamedPipeServerStream stream)
   {
      var serving = ServeConnectionThenDisposeAsync(stream);
      //Tracked so StopAsync can wait for in-flight serves to unwind; the continuation removes it whether it completed before
      //or after this add, so the set never leaks a finished serve nor grows without bound under sustained load.
      _monitor.Locked(() => _connectionsBeingServed.Add(serving));
      serving.ContinueWithCE(_ => _monitor.Locked(() => _connectionsBeingServed.Remove(serving)));
   }

   async Task ServeConnectionThenDisposeAsync(NamedPipeServerStream stream)
   {
      try
      {
         await ServeConnectionAsync(stream).caf();
      }
      finally
      {
         DisposeAndUntrack(stream);
      }
   }

   void DisposeAndUntrack(NamedPipeServerStream stream)
   {
      _monitor.Locked(() => _openStreams.Remove(stream));
      stream.Dispose();
   }

   async Task ServeConnectionAsync(NamedPipeServerStream connection)
   {
      try
      {
         while(!_cancellationSource.IsCancellationRequested)
         {
            TransportRequest request;
            try
            {
               request = await NamedPipeFraming.ReadRequestAsync(connection, _cancellationSource.Token).caf();
            }
            catch(EndOfStreamException) //The client closed the connection: the normal end of every conversation, signaled by the pipe itself.
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
               await NamedPipeFraming.WriteErrorResponseAsync(connection, exception.GetType().FullName!, exception.ToString(), _cancellationSource.Token).caf();
               continue;
            }

            await NamedPipeFraming.WriteSuccessResponseAsync(connection, responsePayload, _cancellationSource.Token).caf();
         }
      }
      catch(OperationCanceledException) {} //Stopping: in-flight conversations are interrupted by our own cancellation token.
      catch(ObjectDisposedException) {} //Stopping: StopAsync disposes open streams to interrupt whatever the token has not yet reached.
      catch(IOException) {} //The client process vanished mid-conversation. A pipe peer disappearing is a real runtime condition no server can prevent; the client side is the one that observes and handles the failure.
   }
}
