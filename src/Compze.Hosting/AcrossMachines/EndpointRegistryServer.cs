using System.Net;
using System.Net.Sockets;
using Compze.Hosting.AcrossMachines._private;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.Transport.Tcp;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Threading.ResourceAccess;

namespace Compze.Hosting.AcrossMachines;

///<summary>A server that keeps endpoint registries for processes on any number of machines: each process opens a registry<br/>
/// on it by name (<see cref="NetworkEndpointRegistry"/>), announces there the addresses its endpoints listen at, and is told<br/>
/// what every other process announced in the registry of that name. It is what <c>InterprocessEndpointRegistry</c> is on<br/>
/// one machine, for endpoints that no shared file can reach.</summary>
///<remarks>What a process announces is in the registry for as long as the process's connection to the server lasts. When<br/>
/// the process ends, the operating system closes its connection, and its addresses leave the registry at once. When its<br/>
/// machine vanishes without closing anything, TCP keepalive ends the connection within about ten seconds. So an address<br/>
/// the server lists is one whose announcing process was reachable seconds ago: the liveness that<br/>
/// <see cref="IEndpointRegistry"/> leaves to the registry.</remarks>
///<remarks>Registries of different names share nothing but the server: a process sees only what was announced in the<br/>
/// registry it opened. The name, with the server's address, is the boundary of an application suite.</remarks>
///<remarks>The server's process must be up for the processes to find each other. A <see cref="NetworkEndpointRegistry"/><br/>
/// opened while it is not keeps trying to reach it, and lists nothing until it does.</remarks>
public sealed class EndpointRegistryServer : IAsyncDisposable
{
   readonly TcpListener _listener;
   readonly CancellationTokenSource _stopping = new();
   readonly IAwaitableThreadShared<NamedRegistries> _registries = IAwaitableThreadShared.New(new NamedRegistries());
   readonly Task _acceptingParticipants;

   ///<summary>Starts a server that listens at <paramref name="listenAt"/>: an IP address of this machine that the other<br/>
   /// machines can connect to, and a port. Port 0 has the operating system pick a free one, which <see cref="ListensAt"/><br/>
   /// then tells.</summary>
   public static EndpointRegistryServer StartListeningAt(IPEndPoint listenAt) => new(listenAt);

   EndpointRegistryServer(IPEndPoint listenAt)
   {
      _listener = new TcpListener(listenAt);
      _listener.Start();
      ListensAt = (IPEndPoint)_listener.LocalEndpoint;
      _acceptingParticipants = TaskCE.Run(AcceptParticipantsUntilStoppedAsync);
   }

   ///<summary>Where the server listens: what a <see cref="NetworkEndpointRegistry"/> is opened with.</summary>
   public IPEndPoint ListensAt { get; }

   ///<summary>Stops the server: every connection to it ends, so every <see cref="NetworkEndpointRegistry"/> opened on it<br/>
   /// lists nothing from then on, and keeps trying to reach a server at the address.</summary>
   public async ValueTask DisposeAsync()
   {
      if(_stopping.IsCancellationRequested) return; //Disposed before.
      await _stopping.CancelAsync().caf();
      await _acceptingParticipants.caf();
      _listener.Dispose();
      _stopping.Dispose();
   }

   async Task AcceptParticipantsUntilStoppedAsync()
   {
      var participantsBeingServed = new List<Task>();
      try
      {
         while(true)
         {
            var connectedSocket = await _listener.AcceptSocketAsync(_stopping.Token).caf();
            participantsBeingServed.RemoveAll(served => served.IsCompletedSuccessfully);
            participantsBeingServed.Add(ServeParticipantOverAsync(connectedSocket));
         }
      }
      catch(OperationCanceledException) when(_stopping.IsCancellationRequested) //The server stops: the cancelled accept is how the loop is told.
      {
      }

      await Task.WhenAll(participantsBeingServed).caf(); //Also rethrows what serving a participant failed with, if it failed with anything but its connection ending.
   }

   async Task ServeParticipantOverAsync(Socket connectedSocket)
   {
      var connection = new RegistryConnection(TcpConnection.StreamOver(connectedSocket));
      await using var _ = connection.ConfigureAwait(false); //Not caf(): through that the analyzer does not see that this disposes the connection.
      await new RegistryParticipant(connection, _registries).ServeUntilTheConnectionEndsAsync(_stopping.Token).caf();
   }
}
