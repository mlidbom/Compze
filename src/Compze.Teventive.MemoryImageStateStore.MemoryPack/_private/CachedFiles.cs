using MemoryPack;

namespace Compze.Teventive.MemoryImageStateStore.MemoryPack._private;

///<summary>The first part of the MemoryPack cache of one class's files: the fingerprint of the snapshot type it was written for, and the<br/>
/// listing of each file whose snapshot follows, in the same order.</summary>
[MemoryPackable]
sealed partial class CachedFiles
{
   ///<summary>The <see cref="SnapshotTypeShape.FingerprintOf"/> of the snapshot type the snapshots were written as.</summary>
   public string Fingerprint { get; init; } = "";

   public List<CachedFileListing> Listings { get; init; } = [];
}
