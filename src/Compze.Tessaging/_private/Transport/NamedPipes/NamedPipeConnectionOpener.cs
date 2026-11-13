using System.IO.Pipes;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Tessaging._private.Transport.FramedConnections;

namespace Compze.Tessaging._private.Transport.NamedPipes;

///<summary>The named-pipe transport's <see cref="IConnectionOpener"/>: connects a pipe to the server listening at a<br/>
/// <see cref="NamedPipeAddress"/>. The pipe is created <see cref="PipeOptions.CurrentUserOnly"/>, so it only connects to a<br/>
/// server running as the same user.</summary>
///<remarks>Windows has no asynchronous named-pipe connect:<br/>
/// <see cref="NamedPipeClientStream.ConnectAsync(int, CancellationToken)"/> runs the fully synchronous connect on a<br/>
/// thread-pool thread and holds that thread until a server instance is available. A connect per send therefore turned N<br/>
/// concurrent sends into N blocked pool threads — and the pool grows by roughly one thread per second once its minimum is<br/>
/// busy, so on a small machine a burst starved the whole process and connects timed out against a perfectly healthy server.<br/>
/// <see cref="FramedConnectionPool"/> is what makes the connect harmless: it bounds how many connects run at once and<br/>
/// amortizes each over the connection's lifetime.</remarks>
///<remarks>A pipe that sits idle does not learn that its server is gone: it finds out when it is next used. So an idle<br/>
/// connection is known to have ended (<see cref="ClientConnection.HasEndedWhileIdle"/>) only if an earlier use found it so.</remarks>
class NamedPipeConnectionOpener : IConnectionOpener
{
   public string TransportName => "named-pipe";

   public async Task<ClientConnection> OpenConnectionAsync(EndpointAddress address, TimeSpan timeout, CancellationToken cancellationToken)
   {
      var pipe = new NamedPipeClientStream(serverName: ".", NamedPipeAddress.PipeNameFrom(address), PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
      try
      {
         await pipe.ConnectAsync((int)timeout.TotalMilliseconds, cancellationToken).caf();
         return new ClientConnection(pipe, hasEndedWhileIdle: () => !pipe.IsConnected);
      }
      catch(TimeoutException exception) //No listening instance took the connection in time: nothing listens at the pipe, or every instance stayed busy.
      {
         await pipe.DisposeAsync().caf();
         throw EndpointUnreachableException.BecauseNoConnectionWasMadeInTime(TransportName, address, timeout, exception);
      }
      catch //Resource cleanup, not handling: the pipe was never handed to the caller, so nothing else will dispose it.
      {
         await pipe.DisposeAsync().caf();
         throw;
      }
   }
}
