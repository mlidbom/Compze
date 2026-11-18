using System.Buffers.Binary;
using System.Collections;
using Compze.Contracts;
using Compze.Teventive.MemoryImageStateStore._internal;
using MemoryPack;
using static Compze.Contracts.Contract;

namespace Compze.Teventive.MemoryImageStateStore.MemoryPack._private;

///<summary>What the MemoryPack cache holds of one class's files, for one load: each file's listing, with the snapshot the file held.</summary>
///<remarks>Its content is the length of a <see cref="CachedFiles"/> in four bytes, the <see cref="CachedFiles"/>, then the snapshots as<br/>
/// one MemoryPack list, in the same order. The listings come first, apart, so that a cache written for another shape of the<br/>
/// snapshot type is known by its fingerprint before any snapshot is read: MemoryPack reads by position, and could read such<br/>
/// snapshots back wrong without failing.</remarks>
sealed class MemoryPackCachedFilesOfAClass : ICachedFilesOfAClass
{
   readonly Type _listOfSnapshots;
   readonly string _fingerprint;
   readonly IReadOnlyDictionary<string, CachedSnapshot> _cached;
   int _snapshotsGiven;

   MemoryPackCachedFilesOfAClass(Type listOfSnapshots, string fingerprint, IReadOnlyDictionary<string, CachedSnapshot> cached)
   {
      _listOfSnapshots = listOfSnapshots;
      _fingerprint = fingerprint;
      _cached = cached;
   }

   ///<summary>What <paramref name="written"/> holds of snapshots of <paramref name="snapshotType"/>: nothing, if it is null, or was<br/>
   /// written for another shape of the type.</summary>
   internal static MemoryPackCachedFilesOfAClass From(byte[]? written, Type snapshotType)
   {
      var fingerprint = SnapshotTypeShape.FingerprintOf(snapshotType);
      var listOfSnapshots = typeof(List<>).MakeGenericType(snapshotType);
      return new(listOfSnapshots, fingerprint, written == null ? new Dictionary<string, CachedSnapshot>() : SnapshotsIn(written, listOfSnapshots, fingerprint));
   }

   public object? SnapshotOf(ListedFile file)
   {
      if(!_cached.TryGetValue(file.RelativePath, out var cached) || !cached.IsOf(file)) return null;
      _snapshotsGiven++;
      return cached.Snapshot;
   }

   public byte[]? ContentAfter(IReadOnlyList<LoadedSnapshot> loaded) => TookEverySnapshotAndOnlyThem(loaded) ? null : ContentHolding(loaded);

   ///<summary>Whether the load took every snapshot the cache holds, and nothing else: it read no file, and found none gone.</summary>
   bool TookEverySnapshotAndOnlyThem(IReadOnlyList<LoadedSnapshot> loaded) => _snapshotsGiven == _cached.Count && _snapshotsGiven == loaded.Count;

   byte[] ContentHolding(IReadOnlyList<LoadedSnapshot> loaded)
   {
      var files = new CachedFiles
                  {
                     Fingerprint = _fingerprint,
                     Listings = [..loaded.Select(it => new CachedFileListing { RelativePath = it.File.RelativePath, Length = it.File.Length, LastWriteTimeUtcTicks = it.File.LastWriteTimeUtc.Ticks })]
                  };
      var snapshots = (IList)Activator.CreateInstance(_listOfSnapshots, loaded.Count)!;
      foreach(var it in loaded) snapshots.Add(it.Snapshot);

      var filesBytes = MemoryPackSerializer.Serialize(files);
      var snapshotsBytes = MemoryPackSerializer.Serialize(_listOfSnapshots, snapshots);
      var content = new byte[sizeof(int) + filesBytes.Length + snapshotsBytes.Length];
      BinaryPrimitives.WriteInt32LittleEndian(content, filesBytes.Length);
      filesBytes.CopyTo(content, sizeof(int));
      snapshotsBytes.CopyTo(content, sizeof(int) + filesBytes.Length);
      return content;
   }

   static Dictionary<string, CachedSnapshot> SnapshotsIn(byte[] written, Type listOfSnapshots, string fingerprint)
   {
      var filesLength = BinaryPrimitives.ReadInt32LittleEndian(written);
      var files = MemoryPackSerializer.Deserialize<CachedFiles>(written.AsSpan(sizeof(int), filesLength))._assert().NotNull();
      if(files.Fingerprint != fingerprint) return new Dictionary<string, CachedSnapshot>();

      var snapshots = (IList)MemoryPackSerializer.Deserialize(listOfSnapshots, written.AsSpan(sizeof(int) + filesLength))._assert().NotNull();
      State.Assert(snapshots.Count == files.Listings.Count, () => $"The cache lists {files.Listings.Count} files and holds {snapshots.Count} snapshots.");
      return files.Listings.Select((listing, index) => new CachedSnapshot(listing, snapshots[index]._assert().NotNull()))
                  .ToDictionary(it => it.Listing.RelativePath, StringComparer.Ordinal);
   }

   ///<summary>A file's listing as the cache holds it, with the snapshot the file held then.</summary>
   sealed class CachedSnapshot
   {
      internal CachedSnapshot(CachedFileListing listing, object snapshot)
      {
         Listing = listing;
         Snapshot = snapshot;
      }

      internal CachedFileListing Listing { get; }

      internal object Snapshot { get; }

      ///<summary>Whether <paramref name="file"/> is the file this is the snapshot of, unchanged: of the same length and last write time.</summary>
      internal bool IsOf(ListedFile file) => file.Length == Listing.Length && file.LastWriteTimeUtc.Ticks == Listing.LastWriteTimeUtcTicks;
   }
}
