using System.Net;
using System.Net.Sockets;
using Compze.Hosting.AcrossMachines._private;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging._internal.Transport.Tcp;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Hosting.AcrossMachines;

///<summary>The across-machines <see cref="IEndpointRegistry"/> and <see cref="IEndpointAddressAnnouncer"/>: a registry kept<br/>
/// by an <see cref="EndpointRegistryServer"/>, as one process sees it. The process's endpoints announce through it the<br/>
/// addresses they listen at, and it lists what every process that opened the registry of the same name on the same server<br/>
/// announced. Nothing but the server's address is configured.</summary>
///<remarks>It keeps one connection to the server, in the background, for as long as it lives. What the process announces<br/>
/// is in the registry while that connection lasts, so the process's end, or its machine vanishing, takes its addresses<br/>
/// out of the registry: see <see cref="EndpointRegistryServer"/>.</remarks>
///<remarks>While it has no connection to the server it lists nothing, since nothing then tells which endpoints are up, and<br/>
/// it keeps trying to connect. That is also how it starts: it can be opened, and announced to, before the server exists.<br/>
/// Once connected it announces everything that was announced to it, and lists what the server tells it. A router waiting<br/>
/// in <see cref="AwaitPossibleMembershipChange"/> wakes when what it lists changes.</remarks>
public sealed class NetworkEndpointRegistry : IEndpointRegistryAndAnnouncer, IAsyncDisposable
{
   //review-naming: NetworkEndpointRegistry, as the name beside InterprocessEndpointRegistry of the registry that reaches across machines. Chosen without asking.
   static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);
   static readonly TimeSpan PauseBetweenAttemptsToConnect = TimeSpan.FromSeconds(1);

   readonly string _registryName;
   readonly EndPoint _registryServerListensAt;
   readonly IAwaitableThreadShared<Participation> _participation = IAwaitableThreadShared.New(new Participation());
   readonly CancellationTokenSource _disposing = new();
   readonly Task _participating;
   bool _theServerIsKnownNotToBeThere;

   ///<summary>Opens the registry named <paramref name="registryName"/> on the <see cref="EndpointRegistryServer"/> that<br/>
   /// listens at <paramref name="registryServerListensAt"/>: an <see cref="IPEndPoint"/>, or a <see cref="DnsEndPoint"/> for a<br/>
   /// server known by its host name. Returns at once, and connects in the background.</summary>
   public static NetworkEndpointRegistry OpenOnServerAt(string registryName, EndPoint registryServerListensAt) => new(registryName, registryServerListensAt);

   NetworkEndpointRegistry(string registryName, EndPoint registryServerListensAt)
   {
      _registryName = registryName;
      _registryServerListensAt = registryServerListensAt;
      _participating = TaskCE.Run(ParticipateUntilDisposedAsync);
   }

   ///<summary>The addresses of every endpoint announced in the registry by a process the server is connected to. None while<br/>
   /// this registry has no connection to the server.</summary>
   public IEnumerable<EndpointAddress> ServerEndpointAddresses => _participation.Read(it => it.Membership.ToList());

   ///<summary>Blocks until the registry's membership differs from what it is when the wait begins, <paramref name="timeout"/><br/>
   /// elapses, or <paramref name="cancellationToken"/> is cancelled. The server tells of every change as it happens, the<br/>
   /// end of another process among them, so a waiting router reconciles at network latency.</summary>
   public void AwaitPossibleMembershipChange(TimeSpan timeout, CancellationToken cancellationToken)
   {
      var membershipAtStart = _participation.Read(it => it.Membership.ToHashSet());
      try
      {
         _participation.TryAwait(it => !it.Membership.SetEquals(membershipAtStart), cancellationToken, new WaitTimeout(timeout));
      }
      catch(OperationCanceledException) //Cancellation is the caller stopping its reconciliation — one of this method's three return conditions, not a failure.
      {
      }
   }

   ///<summary>Announces that the endpoint listens at <paramref name="address"/>, replacing any address it announced before.<br/>
   /// Returns at once: the server is told in the background, now or once it is reached.</summary>
   public void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address) => _participation.Update(it => it.Announce(endpointId, address));

   ///<summary>Retracts the endpoint's announced address — what an endpoint does when it stops listening.</summary>
   public void RetractEndpointAddress(EndpointId endpointId) => _participation.Update(it => it.Retract(endpointId));

   ///<summary>Ends the connection to the server, which takes what this process announced out of the registry.</summary>
   public async ValueTask DisposeAsync()
   {
      if(_disposing.IsCancellationRequested) return; //Disposed before.
      await _disposing.CancelAsync().caf();
      await _participating.caf();
      _disposing.Dispose();
   }

   async Task ParticipateUntilDisposedAsync()
   {
      while(!_disposing.IsCancellationRequested)
      {
         try
         {
            await ParticipateOverOneConnectionAsync().caf();
         }
         catch(SocketException noConnection) //Nothing listens at the address, or the network has no way to it.
         {
            NoteThatTheServerIsNotThere($"no connection could be made: {noConnection.Message}");
         }
         catch(IOException connectionEnded) //The server stopped or its process ended, or its machine vanished and the operating system gave the connection up.
         {
            NoteThatTheServerIsNotThere($"the connection ended: {connectionEnded.Message}");
         }
         catch(OperationCanceledException) when(!_disposing.IsCancellationRequested) //The connect timed out: the server's machine is not there, or a firewall drops the connect.
         {
            NoteThatTheServerIsNotThere($"nothing answered a connect within {ConnectTimeout.TotalSeconds} s");
         }
         catch(OperationCanceledException) when(_disposing.IsCancellationRequested) //This registry is being disposed.
         {
            return;
         }

         await PauseBeforeTheNextAttemptToConnectAsync().caf();
      }
   }

   ///<summary>Connects to the server and converses with it until the connection ends, or this registry is disposed.<br/>
   /// The membership is what the server tells for as long as the conversation lasts, and nothing after it.</summary>
   async Task ParticipateOverOneConnectionAsync()
   {
      using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp); //Closed when this method ends, however it ends. Once connected, the connection over it closes it too, which a socket does not mind.
      await ConnectToTheServerAsync(socket).caf();
      var connection = new RegistryConnection(TcpConnection.StreamOver(socket));
      await using var _ = connection.ConfigureAwait(false); //Not caf(): through that the analyzer does not see that this disposes the connection.
      try
      {
         await connection.SendRegistryNameAsync(_registryName, _disposing.Token).caf();
         NoteThatTheServerIsThere();
         await RegistryConnection.RunBothDirectionsUntilEitherEndsAsync(sending: connectionEnds => SendAnnouncementsWheneverTheyChangeAsync(connection, connectionEnds),
                                                                         receiving: connectionEnds => ReceiveMembershipAsync(connection, connectionEnds),
                                                                         _disposing.Token).caf();
      }
      finally
      {
         _participation.Update(it => it.MembershipIsNoLongerKnown());
      }
   }

   async Task ConnectToTheServerAsync(Socket socket)
   {
      using var timeoutOrDisposal = CancellationTokenSource.CreateLinkedTokenSource(_disposing.Token);
      timeoutOrDisposal.CancelAfter(ConnectTimeout);
      await socket.ConnectAsync(_registryServerListensAt, timeoutOrDisposal.Token).caf();
   }

   async Task SendAnnouncementsWheneverTheyChangeAsync(RegistryConnection connection, CancellationToken connectionEnds)
   {
      long? versionSent = null;
      while(!connectionEnds.IsCancellationRequested)
         versionSent = await SendAnnouncementsOnceTheyAreOfAnotherVersionThanAsync(versionSent, connection, connectionEnds).caf();
   }

   ///<summary>Waits until what this process announces is of another version than <paramref name="versionSent"/>, sends it,<br/>
   /// and returns the version it sent. Sends at once when nothing was sent before.</summary>
   async Task<long> SendAnnouncementsOnceTheyAreOfAnotherVersionThanAsync(long? versionSent, RegistryConnection connection, CancellationToken connectionEnds)
   {
      var (announcements, version) = await _participation.ReadWhenAsync(it => it.AnnouncementsVersion != versionSent,
                                                                        it => (it.Announcements, it.AnnouncementsVersion),
                                                                        connectionEnds).caf();
      await connection.SendAnnouncementsAsync(announcements, connectionEnds).caf();
      return version;
   }

   async Task ReceiveMembershipAsync(RegistryConnection connection, CancellationToken connectionEnds)
   {
      while(!connectionEnds.IsCancellationRequested)
      {
         var membership = await connection.ReceiveAnnouncementsAsync(connectionEnds).caf();
         _participation.Update(it => it.MembershipIs(membership));
      }
   }

   async Task PauseBeforeTheNextAttemptToConnectAsync()
   {
      try
      {
         await Task.Delay(PauseBetweenAttemptsToConnect, _disposing.Token).caf();
      }
      catch(OperationCanceledException) //This registry is being disposed, which the caller's loop sees.
      {
      }
   }

   ///<summary>Logs that the server is not there the first time an attempt finds it so, not on every attempt: an attempt is<br/>
   /// made every second for as long as it is away.</summary>
   void NoteThatTheServerIsNotThere(string why)
   {
      if(_theServerIsKnownNotToBeThere) return;
      _theServerIsKnownNotToBeThere = true;
      this.Log().Warning($"The registry '{_registryName}' has no connection to its registry server at {_registryServerListensAt}: {why}. It lists no endpoints until it has one, and tries to connect every {PauseBetweenAttemptsToConnect.TotalSeconds} s.");
   }

   void NoteThatTheServerIsThere()
   {
      _theServerIsKnownNotToBeThere = false;
      this.Log().Info($"The registry '{_registryName}' is connected to its registry server at {_registryServerListensAt}.");
   }

   ///<summary>This process's participation in the registry: what it announces there, and the registry's membership as the<br/>
   /// server last told it.</summary>
   sealed class Participation
   {
      readonly Dictionary<EndpointId, EndpointAddress> _announcedAddressByEndpoint = new();

      ///<summary>Changes whenever what is announced does: what the sending of announcements compares with the version it sent last.</summary>
      internal long AnnouncementsVersion { get; private set; }

      internal IReadOnlyList<EndpointAnnouncement> Announcements => [.._announcedAddressByEndpoint.Select(it => new EndpointAnnouncement(it.Key, it.Value))];

      ///<summary>The addresses announced in the registry by every process the server is connected to. Empty while it is not known.</summary>
      internal HashSet<EndpointAddress> Membership { get; private set; } = [];

      internal void Announce(EndpointId endpointId, EndpointAddress address)
      {
         _announcedAddressByEndpoint[endpointId] = address;
         AnnouncementsVersion++;
      }

      internal void Retract(EndpointId endpointId)
      {
         _announcedAddressByEndpoint.Remove(endpointId);
         AnnouncementsVersion++;
      }

      internal void MembershipIs(IReadOnlyList<EndpointAnnouncement> everythingAnnouncedInTheRegistry) => Membership = [..everythingAnnouncedInTheRegistry.Select(it => it.Address)];

      internal void MembershipIsNoLongerKnown() => Membership = [];
   }
}
