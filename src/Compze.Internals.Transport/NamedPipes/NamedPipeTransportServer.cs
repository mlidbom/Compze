using System.IO.Pipes;
using Compze.Abstractions.Hosting.Public;
using Compze.Contracts;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;

namespace Compze.Internals.Transport.NamedPipes;

///<summary>The server side of the named-pipe transport: listens on a freshly named pipe, reads<br/>
/// <see cref="TransportRequest"/>s, dispatches each to the handler registered for its<br/>
/// <see cref="TransportRequestKind"/>, and answers with the handler's payload — or, if the handler threw, with an<br/>
/// error response the client rethrows. The same-machine, no-web-stack counterpart of a transport's ASP.NET Core server.</summary>
///<remarks>A handler returns the serialized response payload; transport-level concerns (framing, connection lifecycle,<br/>
/// routing exceptions back to the client) all live here, so handlers contain only "deserialize, execute, serialize".</remarks>
///<remarks>Connections are served by a fixed pool of listener loops, each serving its accepted connection to completion before<br/>
/// accepting the next. The pool bounds concurrent handler executions, and a connection burst beyond it queues in the clients'<br/>
/// pending connects — the transport's natural backpressure.</remarks>
public sealed class NamedPipeTransportServer : IAsyncDisposable
{
   static readonly int ParallelListenerCount = Math.Max(Environment.ProcessorCount * 2, 8);

   readonly IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> _handlers;
   readonly string _pipeName = NamedPipeAddress.NewUniquePipeName();
   readonly CancellationTokenSource _cancellationSource = new();
   readonly IMonitor _monitor = IMonitor.New();
   readonly HashSet<NamedPipeServerStream> _openStreams = [];
   Task[]? _listenerLoops;

   public NamedPipeTransportServer(IReadOnlyDictionary<TransportRequestKind, Func<TransportRequest, Task<string>>> handlers) => _handlers = handlers;

   ///<summary>The address clients connect to; fixed at construction, listening starts at <see cref="StartAsync"/>.</summary>
   public EndpointAddress Address => NamedPipeAddress.CreateLocalAddressForPipe(_pipeName);

   public Task StartAsync()
   {
      State.Assert(_listenerLoops is null);
      _listenerLoops = Enumerable.Range(0, ParallelListenerCount).Select(_ => Task.Run(ListenerLoopAsync)).ToArray();
      this.Log().Info($"Named-pipe transport server listening at {Address.Uri}");
      return Task.CompletedTask;
   }

   public async Task StopAsync()
   {
      if(_listenerLoops is null) return;
      await _cancellationSource.CancelAsync().caf();

      NamedPipeServerStream[] openStreams = [];
      _monitor.Locked(() => openStreams = _openStreams.ToArray());
      foreach(var stream in openStreams) await stream.DisposeAsync().caf();

      await Task.WhenAll(_listenerLoops).caf();
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
         NamedPipeServerStream? stream = null;
         try
         {
#pragma warning disable CA2000 //Ownership is tracked: every created stream is disposed in this loop's finally or, if we are stopping, by the catch below / StopAsync.
            stream = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
#pragma warning restore CA2000
            _monitor.Locked(() => _openStreams.Add(stream));
            await stream.WaitForConnectionAsync(_cancellationSource.Token).caf();
         }
#pragma warning disable CA1031 //The filter makes this narrow: only while stopping — the canceled wait / disposed listener is the shutdown signal itself, not a failure.
         catch(Exception) when(_cancellationSource.IsCancellationRequested)
#pragma warning restore CA1031
         {
            if(stream is not null) DisposeAndUntrack(stream);
            return;
         }

         try
         {
            await ServeConnectionAsync(stream).caf();
         }
         finally
         {
            DisposeAndUntrack(stream);
         }
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
               responsePayload = await _handlers[request.Kind](request).caf();
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
