namespace Compze.Teventive.StateStore._internal;

///<summary>A cache that lets a state store kept in files start without reading every file: for each class, the snapshots its files<br/>
/// held at an earlier start, each with the file's length and last write time, so a file that hasn't changed since isn't read.</summary>
///<remarks>A package of its own plugs one in through <see cref="TaggregateFiles"/>, such as Compze.Teventive.StateStore.MemoryPack:<br/>
/// what can hold a consumer's snapshots fast depends on a serializer the state store doesn't depend on. The cache only turns its<br/>
/// content into snapshots and back: the store reads and writes the cache's file, in its own folder.</remarks>
interface ITaggregateFilesLoadCache
{
   ///<summary>Asserts, where the store is composed, that the cache can hold snapshots of <paramref name="snapshotType"/>.</summary>
   void AssertCanHold(Type snapshotType);

   ///<summary>What the cache holds of one class's files, whose snapshots are of <paramref name="snapshotType"/>: what it wrote at an<br/>
   /// earlier load, <paramref name="written"/>, or nothing if it has written none.</summary>
   ICachedFilesOfAClass Open(byte[]? written, Type snapshotType);
}
