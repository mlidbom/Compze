using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading.ResourceAccess;

namespace Compze.Hosting.AcrossMachines._private;

///<summary>One participant of a registry, as the <see cref="EndpointRegistryServer"/> serves it: the connection from one<br/>
/// <see cref="NetworkEndpointRegistry"/>. What the participant announces is in the registry for as long as the connection<br/>
/// lasts, and leaves it when the connection ends, however it ends.</summary>
sealed class RegistryParticipant
{
   readonly RegistryConnection _connection;
   readonly IAwaitableThreadShared<NamedRegistries> _registries;

   ///<summary>The participant at the other end of <paramref name="connection"/>, which stays its creator's to dispose.</summary>
   internal RegistryParticipant(RegistryConnection connection, IAwaitableThreadShared<NamedRegistries> registries)
   {
      _connection = connection;
      _registries = registries;
   }

   ///<summary>Serves the participant until its connection ends or <paramref name="serverStops"/> is cancelled: learns which<br/>
   /// registry it participates in, then keeps that registry's record of what it announces up to date, and keeps it told of<br/>
   /// the registry's membership.</summary>
   internal async Task ServeUntilTheConnectionEndsAsync(CancellationToken serverStops)
   {
      NamedRegistry? registry = null;
      try
      {
         var registryName = await _connection.ReceiveRegistryNameAsync(serverStops).caf();
         registry = _registries.Update(registries => registries.Named(registryName));
         await RegistryConnection.RunBothDirectionsUntilEitherEndsAsync(sending: connectionEnds => SendMembershipWheneverItChangesAsync(registry, connectionEnds),
                                                                         receiving: connectionEnds => ReceiveAnnouncementsAsync(registry, connectionEnds),
                                                                         serverStops).caf();
      }
      catch(IOException) //The connection ended, which is how a participant leaves: its registry was disposed or its process ended, and the connection closed with it, or its machine vanished and the operating system gave the connection up.
      {
      }
      catch(OperationCanceledException) when(serverStops.IsCancellationRequested) //The server stops, and its participants with it.
      {
      }
      finally
      {
         if(registry != null) _registries.Update(_ => registry.RemoveAnnouncementsOf(this));
      }
   }

   async Task ReceiveAnnouncementsAsync(NamedRegistry registry, CancellationToken connectionEnds)
   {
      while(!connectionEnds.IsCancellationRequested)
      {
         var announcements = await _connection.ReceiveAnnouncementsAsync(connectionEnds).caf();
         _registries.Update(_ => registry.ReplaceAnnouncementsOf(this, announcements));
      }
   }

   async Task SendMembershipWheneverItChangesAsync(NamedRegistry registry, CancellationToken connectionEnds)
   {
      long? versionSent = null;
      while(!connectionEnds.IsCancellationRequested)
         versionSent = await SendMembershipOnceItIsOfAnotherVersionThanAsync(versionSent, registry, connectionEnds).caf();
   }

   ///<summary>Waits until the registry's membership is of another version than <paramref name="versionSent"/>, sends it, and<br/>
   /// returns the version it sent. Sends at once when nothing was sent before.</summary>
   async Task<long> SendMembershipOnceItIsOfAnotherVersionThanAsync(long? versionSent, NamedRegistry registry, CancellationToken connectionEnds)
   {
      var (membership, version) = await _registries.ReadWhenAsync(_ => registry.MembershipVersion != versionSent,
                                                                  _ => (registry.Membership, registry.MembershipVersion),
                                                                  connectionEnds).caf();
      await _connection.SendAnnouncementsAsync(membership, connectionEnds).caf();
      return version;
   }
}
