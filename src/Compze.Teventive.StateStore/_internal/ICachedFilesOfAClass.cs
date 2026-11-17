namespace Compze.Teventive.StateStore._internal;

///<summary>What an <see cref="ITaggregateFilesLoadCache"/> holds of one class's files, for one load of the store.</summary>
interface ICachedFilesOfAClass
{
   ///<summary>The snapshot the cache holds of <paramref name="file"/>, if the file has the length and last write time it had when<br/>
   /// the cache was written. Null if it has another, or the cache holds no snapshot of it.</summary>
   object? SnapshotOf(ListedFile file);

   ///<summary>What the store is to write as the cache, given every file of the class the load found, with its snapshot. Null if the<br/>
   /// load read no file the cache didn't hold, and found none gone: then the cache as written still holds.</summary>
   byte[]? ContentAfter(IReadOnlyList<LoadedSnapshot> loaded);
}
