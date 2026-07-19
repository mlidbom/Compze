using System.Text;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Endpoints.Discovery;
using Compze.Internals.SystemCE.DiagnosticsCE;
using Compze.InterprocessObject;
using Compze.Threading;

namespace Compze.Hosting.SameMachine;

///<summary>The same-machine <see cref="IEndpointRegistry"/> and <see cref="IEndpointAddressAnnouncer"/>: endpoints announce the<br/>
/// address they listen on, and every process on the machine that opens the registry — same name, same directory — sees it. Backed by<br/>
/// an <see cref="IInterprocessObject{T}"/>, so announcements are immediately visible across processes with no server or configuration,<br/>
/// survive process restarts, and signal every waiting reader the moment they land: a router blocked in<br/>
/// <see cref="AwaitPossibleMembershipChange"/> — in any process sharing the registry — wakes and reconciles at signal latency<br/>
/// (tens of milliseconds at most) when an endpoint announces or retracts, instead of waiting out its periodic pass.</summary>
///<remarks>The backing file outlives crashed processes by design, and a crashed process's addresses must never be routed to — so every<br/>
/// entry records the <see cref="ProcessIdentity"/> of its announcing process, addresses whose announcing process is no longer running are<br/>
/// invisible to readers, and each announcement prunes them from the file.</remarks>
public class InterprocessEndpointRegistry : IEndpointRegistryAndAnnouncer, IDisposable
{
   const int MaxRegistryBytes = 64 * 1024;
   readonly IInterprocessObject<RegistryState> _sharedState;

   ///<summary>Opens — creating it if it does not yet exist — the registry named <paramref name="registryName"/> in <paramref name="directory"/>,<br/>
   /// shared with every process in the same login session that opens the same name and directory. The name and directory ARE the application-suite<br/>
   /// boundary: processes that should discover each other's endpoints open the same registry; unrelated applications use their own.</summary>
   public static InterprocessEndpointRegistry OpenOrCreateSessionLocal(string registryName, DirectoryInfo directory) => new(registryName, directory);

   InterprocessEndpointRegistry(string registryName, DirectoryInfo directory) =>
      _sharedState = IInterprocessObject.NewLocal(registryName,
                                                  new RegistryState.Serializer(),
                                                  createDefault: () => new RegistryState(),
                                                  CorruptionAction.ReplaceContentWithDefaultAndThrow,
                                                  MaxRegistryBytes,
                                                  directory);

   ///<summary>The addresses of every announced endpoint whose announcing process is still running.</summary>
   public IEnumerable<EndpointAddress> ServerEndpointAddresses =>
      _sharedState.Read(state => LiveAddressUris(state).Select(uri => new EndpointAddress(new Uri(uri))).ToList());

   ///<summary>Blocks until the registry's live membership differs from what it is when the wait begins, <paramref name="timeout"/><br/>
   /// elapses, or <paramref name="cancellationToken"/> is cancelled. Every announcement and retraction — from any process sharing<br/>
   /// the registry — raises the backing <see cref="IInterprocessObject{T}"/>'s cross-process signal, and the membership condition is<br/>
   /// re-read on each signal; a write that leaves membership unchanged (an endpoint re-announcing its existing address) keeps waiting.<br/>
   /// A crashed process raises no signal — its addresses just stop being listed — which is one reason the caller's periodic pass exists.</summary>
   public void AwaitPossibleMembershipChange(TimeSpan timeout, CancellationToken cancellationToken)
   {
      var membershipAtStart = _sharedState.Read(LiveAddressUris);
      try
      {
         _sharedState.TryAwait(state => !LiveAddressUris(state).SetEquals(membershipAtStart), cancellationToken, new WaitTimeout(timeout));
      }
      catch(OperationCanceledException) //Cancellation is the caller stopping its reconciliation — one of this method's three return conditions, not a failure.
      {
      }
   }

   ///<summary>The address uris of every announced endpoint whose announcing process is still running — the registry's membership as readers see it.</summary>
   static HashSet<string> LiveAddressUris(RegistryState state) =>
      [..state.Entries.Where(entry => entry.AnnouncingProcess.IsCurrentlyRunning).Select(entry => entry.AddressUri)];

   ///<summary>Announces <paramref name="address"/> as where the endpoint listens, on behalf of this process.</summary>
   public void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address) => AnnounceEndpointAddress(endpointId, address, ProcessIdentity.OfCurrentProcess);

   ///<summary>Announces <paramref name="address"/> as where the endpoint listens, replacing any address the endpoint announced before.<br/>
   /// Also prunes every entry whose announcing process has exited — announcement is the registry's self-cleaning moment.</summary>
   public void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address, ProcessIdentity announcingProcess) =>
      _sharedState.Update(state =>
      {
         state.Entries.RemoveAll(entry => entry.EndpointId == endpointId.Value || !entry.AnnouncingProcess.IsCurrentlyRunning);
         state.Entries.Add(new RegistryState.Entry(endpointId.Value, address.Uri.AbsoluteUri, announcingProcess));
      });

   ///<summary>Retracts the endpoint's announced address — what an endpoint does when it stops listening.</summary>
   public void RetractEndpointAddress(EndpointId endpointId) =>
      _sharedState.Update(state => state.Entries.RemoveAll(entry => entry.EndpointId == endpointId.Value));

   ///<summary>Deletes the backing file from disk, destroying the registry for every process sharing it.</summary>
   public void Delete() => _sharedState.Delete();

   public void Dispose() => _sharedState.Dispose();

   class RegistryState
   {
      internal List<Entry> Entries { get; } = [];

      internal class Entry(Guid endpointId, string addressUri, ProcessIdentity announcingProcess)
      {
         internal Guid EndpointId { get; } = endpointId;
         internal string AddressUri { get; } = addressUri;
         internal ProcessIdentity AnnouncingProcess { get; } = announcingProcess;
      }

      internal class Serializer : IInterprocessObjectSerializer<RegistryState>
      {
         public byte[] Serialize(RegistryState instance)
         {
            using var buffer = new MemoryStream();
            using var writer = new BinaryWriter(buffer, Encoding.UTF8);
            writer.Write(instance.Entries.Count);
            foreach(var entry in instance.Entries)
            {
               writer.Write(entry.EndpointId.ToByteArray());
               writer.Write(entry.AddressUri);
               writer.Write(entry.AnnouncingProcess.ProcessId);
               writer.Write(entry.AnnouncingProcess.StartTime.Ticks);
            }

            writer.Flush();
            return buffer.ToArray();
         }

         public RegistryState Deserialize(byte[] data)
         {
            using var buffer = new MemoryStream(data);
            using var reader = new BinaryReader(buffer, Encoding.UTF8);
            var state = new RegistryState();
            var entryCount = reader.ReadInt32();
            for(var index = 0; index < entryCount; index++)
            {
               state.Entries.Add(new Entry(endpointId: new Guid(reader.ReadBytes(16)),
                                           addressUri: reader.ReadString(),
                                           announcingProcess: new ProcessIdentity(reader.ReadInt32(), new DateTime(reader.ReadInt64(), DateTimeKind.Utc))));
            }

            return state;
         }
      }
   }
}
