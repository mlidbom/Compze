namespace Compze.Teventive.StateStore._internal;

///<summary>A file a load found, and the snapshot it holds: read from the file, or from the cache when the file hadn't changed.</summary>
sealed class LoadedSnapshot
{
   internal LoadedSnapshot(ListedFile file, object snapshot)
   {
      File = file;
      Snapshot = snapshot;
   }

   internal ListedFile File { get; }

   internal object Snapshot { get; }
}
