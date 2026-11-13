using System.IO.Pipes;
using Compze.Contracts;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Threading;
using Compze.Tessaging._private.Transport.FramedConnections;

namespace Compze.Tessaging._private.Transport.NamedPipes;

///<summary>The named-pipe transport's <see cref="IConnectionListener"/>: listens on a freshly named pipe, created<br/>
/// <see cref="PipeOptions.CurrentUserOnly"/> so that only processes running as the same user can connect.</summary>
///<remarks>A fixed set of listening instances stays ready to accept: a pipe accepts one connection per instance, so each<br/>
/// instance that accepts a connection hands it off to be served and is at once replaced by a new instance. No instance ever<br/>
/// waits on a handler, so every one of them stays ready to accept even under a burst of clients.</remarks>
sealed class NamedPipeConnectionListener : IConnectionListener
{
   static readonly int ListeningInstanceCount = Math.Max(Environment.ProcessorCount * 2, 8);

   readonly string _pipeName = NamedPipeAddress.NewUniquePipeName();
   readonly CancellationTokenSource _cancellationSource = new();
   readonly IMonitor _monitor = IMonitor.New();
   readonly HashSet<NamedPipeServerStream> _instancesWaitingForAConnection = [];
   Task[]? _listenerLoops;

   ///<summary>Fixed at construction, although listening starts at <see cref="StartListening"/>: the pipe's name is this listener's own choice.</summary>
   public EndpointAddress Address => NamedPipeAddress.CreateLocalAddressForPipe(_pipeName);

   public void StartListening(Action<Stream> serveConnection)
   {
      State.Assert(_listenerLoops is null);
      _listenerLoops = [..Enumerable.Range(0, ListeningInstanceCount).Select(_ => TaskCE.Run(() => ListenerLoopAsync(serveConnection)))];
   }

   public async Task StopListeningAsync()
   {
      if(_listenerLoops is null) return;
      await _cancellationSource.CancelAsync().caf();

      //Disposing the instances still waiting for a connection interrupts the waits the token has not yet reached, so the loops unwind on the cancellation.
      var instancesWaitingForAConnection = _monitor.Locked<NamedPipeServerStream[]>(() => [.._instancesWaitingForAConnection]);
      foreach(var instance in instancesWaitingForAConnection) await instance.DisposeAsync().caf();

      await Task.WhenAll(_listenerLoops).caf();
      _listenerLoops = null;
   }

   public async ValueTask DisposeAsync()
   {
      await StopListeningAsync().caf();
      _cancellationSource.Dispose();
   }

   async Task ListenerLoopAsync(Action<Stream> serveConnection)
   {
      while(!_cancellationSource.IsCancellationRequested)
      {
         var connection = await AcceptConnectionOrNullWhenStoppingAsync().caf();
         if(connection is null) return;

         //Handing the connection off and looping back at once to create the next listening instance is what keeps every
         //instance ready to accept: none of them ever waits on a handler.
         serveConnection(connection);
      }
   }

   ///<summary>Creates a listening instance and waits for a client to connect to it: the instance is the connection then,<br/>
   /// and the caller owns it. Null when the listener stopped while the instance waited.</summary>
   async Task<NamedPipeServerStream?> AcceptConnectionOrNullWhenStoppingAsync()
   {
      var instance = CreateAndTrackListeningInstance();
      try
      {
         await instance.WaitForConnectionAsync(_cancellationSource.Token).caf();
      }
#pragma warning disable CA1031 //The filter makes this narrow: only while stopping — the canceled wait / disposed instance is the shutdown signal itself, not a failure.
      catch(Exception) when(_cancellationSource.IsCancellationRequested)
#pragma warning restore CA1031
      {
         DisposeAndUntrack(instance);
         return null;
      }
      catch //Resource cleanup, not handling: the instance never became a connection, so nothing else will dispose it.
      {
         DisposeAndUntrack(instance);
         throw;
      }

      _monitor.Locked(() => _instancesWaitingForAConnection.Remove(instance));
      return instance;
   }

   NamedPipeServerStream CreateAndTrackListeningInstance()
   {
#pragma warning disable CA2000 //Ownership is tracked: an instance that accepts a connection is returned to the listener loop, which hands it to the server; one still waiting when we stop is disposed by StopListeningAsync or by the wait's catch.
      var instance = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
#pragma warning restore CA2000
      _monitor.Locked(() => _instancesWaitingForAConnection.Add(instance));
      return instance;
   }

   void DisposeAndUntrack(NamedPipeServerStream instance)
   {
      _monitor.Locked(() => _instancesWaitingForAConnection.Remove(instance));
      instance.Dispose();
   }
}
