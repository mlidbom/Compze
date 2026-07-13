using System.Text;
using Compze.Abstractions.Hosting.Public;
using Compze.InterprocessObject;

namespace Compze.Hosting.SameMachine;

///<summary>The same-machine <see cref="IEndpointRegistry"/> and <see cref="IEndpointAddressAnnouncer"/>: endpoints announce the<br/>
/// address they listen on, and every process on the machine that opens the registry — same name, same directory — sees it. Backed by<br/>
/// an <see cref="IInterprocessObject{T}"/>, so announcements are immediately visible across processes with no server or configuration,<br/>
/// and survive process restarts.</summary>
///<remarks>The backing file outlives crashed processes by design, and a crashed process's addresses must never be routed to — so every<br/>
/// entry records its <see cref="AnnouncingProcess"/>, addresses whose announcing process is no longer running are invisible to readers,<br/>
/// and each announcement prunes them from the file.</remarks>
public class InterprocessEndpointRegistry : IEndpointRegistry, IEndpointAddressAnnouncer, IDisposable
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
      _sharedState.Read(state => state.Entries
                                      .Where(entry => entry.AnnouncingProcess.IsStillRunning)
                                      .Select(entry => new EndpointAddress(new Uri(entry.AddressUri)))
                                      .ToList());

   ///<summary>Announces <paramref name="address"/> as where the endpoint listens, on behalf of this process.</summary>
   public void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address) => AnnounceEndpointAddress(endpointId, address, AnnouncingProcess.Current);

   ///<summary>Announces <paramref name="address"/> as where the endpoint listens, replacing any address the endpoint announced before.<br/>
   /// Also prunes every entry whose announcing process has exited — announcement is the registry's self-cleaning moment.</summary>
   public void AnnounceEndpointAddress(EndpointId endpointId, EndpointAddress address, AnnouncingProcess announcingProcess) =>
      _sharedState.Update(state =>
      {
         state.Entries.RemoveAll(entry => entry.EndpointId == endpointId.Value || !entry.AnnouncingProcess.IsStillRunning);
         state.Entries.Add(new RegistryState.Entry(endpointId.Value, address.Uri.AbsoluteUri, announcingProcess.ProcessId, announcingProcess.StartTimeTicks));
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

      internal class Entry(Guid endpointId, string addressUri, int announcingProcessId, long announcingProcessStartTimeTicks)
      {
         internal Guid EndpointId { get; } = endpointId;
         internal string AddressUri { get; } = addressUri;
         internal AnnouncingProcess AnnouncingProcess { get; } = new(announcingProcessId, announcingProcessStartTimeTicks);
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
               writer.Write(entry.AnnouncingProcess.StartTimeTicks);
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
                                           announcingProcessId: reader.ReadInt32(),
                                           announcingProcessStartTimeTicks: reader.ReadInt64()));
            }

            return state;
         }
      }
   }
}
