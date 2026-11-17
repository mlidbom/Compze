using MemoryPack;

namespace Compze.Teventive.StateStore.MemoryPack._private;

///<summary>A file of a class as the cache lists it: where it is, relative to the class's folder, and its length and last write time<br/>
/// when its snapshot was cached.</summary>
[MemoryPackable]
sealed partial class CachedFileListing
{
   public string RelativePath { get; init; } = "";

   public long Length { get; init; }

   public long LastWriteTimeUtcTicks { get; init; }
}
